// A tiny mock of Bookstore.Api for the "State Management in Angular" series: no framework, no database.
// Run: npm run api   (PORT 5280, DELAY_MS 300)
// FAIL="POST /api/books/9780441172719/reviews:1" makes the first matching request answer 503 (comma-separated rules).
import { createServer, type IncomingMessage, type ServerResponse } from "node:http";
import { books, categories, coupons, reviews, type Review } from "../data/books.ts";

const port = Number(process.env.PORT ?? 5280);
const delayMs = Number(process.env.DELAY_MS ?? 300);
const pageSize = 2;
const failRules = new Map(
  (process.env.FAIL ?? "").split(",").filter(Boolean).map((rule) => {
    const at = rule.lastIndexOf(":");
    return [rule.slice(0, at), Number(rule.slice(at + 1))] as const;
  }),
);
let requestNo = 0;
let nextReviewId = 4;
let nextOrderId = 1043;

const server = createServer((req, res) => {
  const no = ++requestNo;
  const url = new URL(req.url ?? "/", `http://localhost:${port}`);
  const route = `${req.method} ${url.pathname}`;
  console.log(`[api] #${no} ${req.method} ${url.pathname}${url.search}`);
  const cors = {
    "Access-Control-Allow-Origin": "*",
    "Access-Control-Allow-Methods": "GET, POST",
    "Access-Control-Allow-Headers": "Content-Type",
  };
  if (req.method === "OPTIONS") {
    res.writeHead(204, cors).end();
    return;
  }
  setTimeout(() => void handle(req, res, url, route, no, cors), delayMs);
});

async function handle(
  req: IncomingMessage,
  res: ServerResponse,
  url: URL,
  route: string,
  no: number,
  cors: Record<string, string>,
) {
  if (res.destroyed) return;
  const send = (status: number, body?: unknown) => {
    res.writeHead(status, { ...cors, "Content-Type": "application/json" }).end(body === undefined ? "" : JSON.stringify(body));
    console.log(`[api] #${no} ${status}`);
  };
  const left = failRules.get(route) ?? 0;
  if (left > 0) {
    failRules.set(route, left - 1);
    return send(503, { title: "Service Unavailable", status: 503 });
  }

  if (route === "GET /api/categories") return send(200, categories);

  if (route === "GET /api/books") {
    // ?category=classics&q=emm&sort=price&page=2
    const category = url.searchParams.get("category");
    const q = url.searchParams.get("q")?.toLowerCase();
    const sort = url.searchParams.get("sort") === "price" ? "price" : "title";
    const page = Math.max(1, Number(url.searchParams.get("page")) || 1);
    const all = books
      .filter((b) => !category || b.category === category)
      .filter((b) => !q || b.title.toLowerCase().includes(q))
      .toSorted((a, b) => (sort === "price" ? a.price - b.price : a.title.localeCompare(b.title)));
    return send(200, { items: all.slice((page - 1) * pageSize, page * pageSize), total: all.length, page, pageSize });
  }

  const bookMatch = url.pathname.match(/^\/api\/books\/(\d{13})(\/reviews)?$/);
  const book = books.find((b) => b.isbn === bookMatch?.[1]);
  if (book && req.method === "GET" && !bookMatch?.[2]) return send(200, book);
  if (book && req.method === "GET") return send(200, reviews.filter((r) => r.isbn === book.isbn));
  if (book && req.method === "POST" && bookMatch?.[2]) {
    const { author = "", rating = 0, text = "" } = await readJson<Partial<Review>>(req);
    if (text.trim().length < 3 || !(rating >= 1 && rating <= 5)) {
      return send(400, { title: "One or more validation errors occurred.", status: 400 });
    }
    const review: Review = { id: nextReviewId++, isbn: book.isbn, author, rating, text };
    reviews.push(review);
    return send(201, review);
  }

  if (route === "POST /api/orders") {
    // another customer's order changes the stock that every open page has already loaded
    const { items = [] } = await readJson<{ items?: { isbn: string; quantity: number }[] }>(req);
    for (const item of items) {
      const b = books.find((x) => x.isbn === item.isbn);
      if (!b || item.quantity > b.stock) return send(409, { title: "Not enough stock", status: 409 });
    }
    for (const item of items) books.find((x) => x.isbn === item.isbn)!.stock -= item.quantity;
    return send(201, { id: nextOrderId++ });
  }

  const couponMatch = url.pathname.match(/^\/api\/coupons\/([A-Z0-9]+)$/);
  if (couponMatch && req.method === "GET") {
    const coupon = coupons.find((c) => c.code === couponMatch[1]);
    return coupon ? send(200, coupon) : send(404, { title: "Unknown coupon", status: 404 });
  }

  send(404, { title: "Not Found", status: 404 });
}

async function readJson<T>(req: IncomingMessage): Promise<T> {
  let raw = "";
  for await (const chunk of req) raw += chunk;
  return JSON.parse(raw || "{}") as T;
}

server.listen(port, () => console.log(`[api] listening on http://localhost:${port}`));
