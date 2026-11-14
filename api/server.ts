// A small mock of Bookstore.Api for the Browser APIs and PWA series: no framework, no database.
// Run: node api/server.ts   (PORT defaults to 5281; serves web/ as static files)
import { createHash } from "node:crypto";
import { readFileSync } from "node:fs";
import { readFile } from "node:fs/promises";
import { createServer, type IncomingMessage, type ServerResponse } from "node:http";
import { createSecureServer } from "node:http2";
import { extname, join, normalize, resolve } from "node:path";
import { books, customers } from "../data/books.ts";

const port = Number(process.env.PORT ?? 5281);
const webRoot = new URL("../web/", import.meta.url).pathname;
let requestNo = 0;
// 03: reviews and the responses already given per Idempotency-Key
let nextReviewId = 501;
const idempotent = new Map<string, { status: number; body: unknown }>();

const types: Record<string, string> = {
  ".html": "text/html; charset=utf-8",
  ".js": "text/javascript; charset=utf-8",
  ".css": "text/css; charset=utf-8",
  ".json": "application/json",
  ".webmanifest": "application/manifest+json",
  ".png": "image/png",
  ".ico": "image/x-icon",
};

const sleep = (ms: number) => new Promise((resolve) => setTimeout(resolve, ms));

function log(no: number, message: string) {
  console.log(`[api] #${no} ${message}`);
}

async function readBody(req: IncomingMessage) {
  let bytes = 0;
  let chunks = 0;
  const parts: Buffer[] = [];
  for await (const chunk of req) {
    bytes += chunk.length;
    chunks++;
    parts.push(chunk);
  }
  return { bytes, chunks, text: Buffer.concat(parts).toString("utf8") };
}

function sendJson(req: IncomingMessage, res: ServerResponse, no: number, body: unknown, cacheControl = "no-store") {
  const json = JSON.stringify(body);
  const etag = `"${createHash("sha1").update(json).digest("hex").slice(0, 12)}"`;
  res.setHeader("Cache-Control", cacheControl);
  res.setHeader("ETag", etag);
  if (req.headers["if-none-match"] === etag) {
    res.writeHead(304).end();
    log(no, "304 Not Modified");
    return;
  }
  res.writeHead(200, { "Content-Type": "application/json" }).end(json);
  log(no, `200 ${json.length} bytes`);
}

// 01: the order report as CSV, written in chunks of 500 rows so the client can process it while it downloads
async function ordersCsv(res: ServerResponse, no: number, rows: number) {
  res.writeHead(200, { "Content-Type": "text/csv; charset=utf-8", "Cache-Control": "no-store" });
  res.write("orderId,customer,isbn,quantity,total\n");
  for (let i = 0; i < rows; i += 500) {
    let chunk = "";
    for (let j = i; j < Math.min(i + 500, rows); j++) {
      const book = books[j % books.length];
      const quantity = (j % 3) + 1;
      chunk += `${100000 + j},${customers[j % customers.length]},${book.isbn},${quantity},${book.price * quantity}\n`;
    }
    if (res.destroyed) return;
    res.write(chunk);
    await sleep(20);
  }
  res.end();
  log(no, `200 csv ${rows} rows`);
}

// 03: POST /api/books/:isbn/reviews needs an Idempotency-Key; a repeated key gets the first response again
async function addReview(req: IncomingMessage, res: ServerResponse, no: number, isbn: string) {
  const key = req.headers["idempotency-key"];
  const { text } = await readBody(req);
  if (typeof key !== "string") {
    res.writeHead(400, { "Content-Type": "application/json" }).end(JSON.stringify({ title: "Idempotency-Key header is required" }));
    log(no, "400 no Idempotency-Key");
    return;
  }
  const previous = idempotent.get(key);
  if (previous) {
    res.writeHead(previous.status, { "Content-Type": "application/json", "Idempotency-Replayed": "true" });
    res.end(JSON.stringify(previous.body));
    log(no, `${previous.status} replayed for key ${key.slice(0, 8)}`);
    return;
  }
  const { rating, comment } = JSON.parse(text);
  const review = { id: nextReviewId++, isbn, rating, comment };
  idempotent.set(key, { status: 201, body: review });
  res.writeHead(201, { "Content-Type": "application/json" }).end(JSON.stringify(review));
  log(no, `201 review ${review.id} for ${isbn} (key ${key.slice(0, 8)})`);
}

// 05: --app <folder> (or APP_DIST) serves the Angular production build instead of web/, with the cache headers a deploy needs
const appArg = process.argv.indexOf("--app");
const appFolder = appArg > 0 ? process.argv[appArg + 1] : process.env.APP_DIST;
const appDist = appFolder ? `${resolve(appFolder)}/` : null;

function cacheControlFor(pathname: string) {
  if (appDist && /-[\w-]{8}\.(js|css)$/.test(pathname)) return "public, max-age=31536000, immutable"; // hashed bundles never change
  // 04: the service worker script gets a long max-age, like a careless "cache all .js for an hour" rule
  if (pathname === "/sw/sw.js") return "max-age=3600";
  return "no-cache"; // index.html, ngsw.json, ngsw-worker.js, manifest: always ask the server
}

async function serveStatic(res: ServerResponse, no: number, pathname: string) {
  const root = appDist ?? webRoot;
  let file = normalize(join(root, pathname.endsWith("/") ? `${pathname}index.html` : pathname));
  if (!file.startsWith(root)) {
    res.writeHead(403).end();
    return;
  }
  // SPA fallback: an Angular route such as /books/9780441172719 gets index.html
  if (appDist && !extname(pathname)) file = join(root, "index.html");
  try {
    const body = await readFile(file);
    res.writeHead(200, { "Content-Type": types[extname(file)] ?? "application/octet-stream", "Cache-Control": cacheControlFor(pathname) });
    res.end(body);
    log(no, `200 ${pathname}`);
  } catch {
    res.writeHead(404).end();
    log(no, `404 ${pathname}`);
  }
}

async function handle(req: IncomingMessage, res: ServerResponse) {
  const no = ++requestNo;
  const url = new URL(req.url ?? "/", `http://localhost:${port}`);
  const conditional = req.headers["if-none-match"] ? " (If-None-Match)" : "";
  const h2 = req.httpVersion === "2.0" ? " HTTP/2" : "";
  const preload = req.headers["service-worker-navigation-preload"] ? " (navigation preload)" : "";
  log(no, `${req.method} ${url.pathname}${url.search}${conditional}${h2}${preload}`);
  res.on("close", () => {
    if (!res.writableFinished) log(no, "client aborted");
  });

  const delay = Number(url.searchParams.get("delay") ?? 0);
  if (delay) await sleep(delay);
  if (res.destroyed) return;

  if (req.method === "GET" && url.pathname === "/api/books") {
    sendJson(req, res, no, books, "max-age=60");
  } else if (req.method === "GET" && /^\/api\/books\/\d{13}$/.test(url.pathname)) {
    const book = books.find((b) => b.isbn === url.pathname.split("/")[3]);
    if (book) sendJson(req, res, no, book, "no-cache");
    else {
      res.writeHead(404).end();
      log(no, "404");
    }
  } else if (req.method === "POST" && /^\/api\/books\/\d{13}\/reviews$/.test(url.pathname)) {
    await addReview(req, res, no, url.pathname.split("/")[3]);
  } else if (req.method === "GET" && url.pathname === "/api/me/orders") {
    // 04: a per-user response; ?vary=1 adds Vary: Authorization
    const customer = (req.headers.authorization ?? "").replace("Bearer ", "") || "anonymous";
    if (url.searchParams.get("vary")) res.setHeader("Vary", "Authorization");
    sendJson(req, res, no, { customer, orders: customer === "ada" ? [1042] : [1051] });
  } else if (req.method === "GET" && url.pathname === "/api/reports/orders.csv") {
    await ordersCsv(res, no, Number(url.searchParams.get("rows") ?? 20000));
  } else if (req.method === "POST" && url.pathname.startsWith("/api/covers/")) {
    // 01: cover upload; reports how many bytes and chunks arrived
    const { bytes, chunks } = await readBody(req);
    const isbn = url.pathname.split("/").pop();
    sendJson(req, res, no, { isbn, bytes, chunks });
  } else if (req.method === "POST" && url.pathname === "/api/events") {
    // 01: analytics events from sendBeacon, keepalive fetch and fetchLater
    const { bytes, text } = await readBody(req);
    res.writeHead(204).end();
    log(no, `204 event ${bytes} bytes ${req.headers["content-type"] ?? ""} ${text.slice(0, 60)}`);
  } else if (req.method === "GET" && !url.pathname.startsWith("/api/")) {
    await serveStatic(res, no, url.pathname);
  } else {
    res.writeHead(404).end();
    log(no, "404");
  }
}

function safeHandle(req: IncomingMessage, res: ServerResponse) {
  handle(req, res).catch((err) => console.log(`[api] request failed: ${err.message}`));
}

// TLS_KEY and TLS_CERT (PEM files) switch to HTTPS with HTTP/2; streaming uploads need HTTP/2 in Chromium (01)
const server = process.env.TLS_KEY && process.env.TLS_CERT
  ? createSecureServer({ key: readFileSync(process.env.TLS_KEY), cert: readFileSync(process.env.TLS_CERT), allowHTTP1: true }, safeHandle as never)
  : createServer(safeHandle);

const scheme = process.env.TLS_KEY ? "https" : "http";
server.listen(port, "127.0.0.1", () => console.log(`[api] listening on ${scheme}://localhost:${port}`));
