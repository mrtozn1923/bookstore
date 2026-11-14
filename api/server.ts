// A tiny mock of Bookstore.Api for the RxJS series: no framework, no database.
// Run: node api/server.ts   (PORT defaults to 5180)
import { randomBytes } from "node:crypto";
import { createServer, type IncomingMessage, type ServerResponse } from "node:http";
import { books, orders } from "../data/books.ts";

const port = Number(process.env.PORT ?? 5180);
const delayMs = Number(process.env.DELAY_MS ?? 200);
const failures = new Map<string, number>();
// FAIL=/api/books:2 makes the first 2 requests to /api/books answer 503 (same as ?fail=2, for apps that cannot add it)
const failFromEnv = new Map(
  (process.env.FAIL ?? "").split(",").filter(Boolean).map((rule) => {
    const [path, count] = rule.split(":");
    return [path, Number(count)] as const;
  }),
);
let nextOrderId = 1043;
const statusPolls = new Map<number, number>();
let requestNo = 0;

const server = createServer((req, res) => {
  const no = ++requestNo;
  const url = new URL(req.url ?? "/", `http://localhost:${port}`);
  console.log(`[api] #${no} ${req.method} ${url.pathname}${url.search}`);

  // CORS preflight for POST requests with a JSON body (09)
  if (req.method === "OPTIONS") {
    res.writeHead(204, {
      "Access-Control-Allow-Origin": "*",
      "Access-Control-Allow-Methods": "GET, POST",
      "Access-Control-Allow-Headers": "Content-Type",
    }).end();
    console.log(`[api] #${no} 204 preflight`);
    return;
  }

  res.on("close", () => {
    if (!res.writableFinished) console.log(`[api] #${no} client aborted`);
  });

  setTimeout(() => {
    if (res.destroyed) return;
    res.setHeader("Access-Control-Allow-Origin", "*");

    // ?fail=N: the first N requests to this path answer 503
    const fail = Number(url.searchParams.get("fail") ?? failFromEnv.get(url.pathname) ?? 0);
    const seen = failures.get(url.pathname) ?? 0;
    if (seen < fail) {
      failures.set(url.pathname, seen + 1);
      res.writeHead(503).end();
      console.log(`[api] #${no} 503`);
      return;
    }

    if (req.method === "POST" && url.pathname === "/api/orders") {
      void placeOrder(req, res, no);
      return;
    }

    // order status for polling (06): Paid on the first 3 requests, then Shipped twice, then Delivered
    const orderMatch = url.pathname.match(/^\/api\/orders\/(\d+)\/status$/);
    const order = orders.find((o) => o.id === Number(orderMatch?.[1]));
    if (order) {
      const polls = (statusPolls.get(order.id) ?? 0) + 1;
      statusPolls.set(order.id, polls);
      const status = polls <= 3 ? "Paid" : polls <= 5 ? "Shipped" : "Delivered";
      res.writeHead(200, { "Content-Type": "application/json" }).end(JSON.stringify({ id: order.id, status }));
      console.log(`[api] #${no} 200 ${status}`);
      return;
    }

    // categories (08): the distinct categories of the sample books
    if (url.pathname === "/api/categories") {
      const categories = [...new Set(books.map((b) => b.category))];
      res.writeHead(200, { "Content-Type": "application/json" }).end(JSON.stringify(categories));
      console.log(`[api] #${no} 200`);
      return;
    }

    const match = url.pathname.match(/^\/api\/books(?:\/(\d{13}))?(?:\/(stock|price))?$/);
    const book = match?.[1] ? books.find((b) => b.isbn === match[1]) : undefined;
    let body: unknown;
    // ?q=dun (10): title search, case-insensitive
    const q = url.searchParams.get("q")?.toLowerCase();
    if (match && !match[1]) body = q ? books.filter((b) => b.title.toLowerCase().includes(q)) : books;
    else if (book && match?.[2] === "stock") body = { isbn: book.isbn, stock: book.stock };
    else if (book && match?.[2] === "price") body = { isbn: book.isbn, price: book.price, currency: "TRY" };
    else if (book) body = book;

    if (body === undefined) {
      res.writeHead(404).end();
      console.log(`[api] #${no} 404`);
      return;
    }
    res.writeHead(200, { "Content-Type": "application/json" }).end(JSON.stringify(body));
    console.log(`[api] #${no} 200`);
  }, delayMs);
});

// place an order (09): answers like ASP.NET Core's TypedResults.Problem / ValidationProblem
async function placeOrder(req: IncomingMessage, res: ServerResponse, no: number) {
  let raw = "";
  for await (const chunk of req) raw += chunk;
  const { items = [] } = JSON.parse(raw || "{}") as { items?: { isbn: string; quantity: number }[] };
  const traceId = `00-${randomBytes(16).toString("hex")}-${randomBytes(8).toString("hex")}-00`;
  const problem = (status: number, { type, title, ...rest }: { type: string; title: string; [key: string]: unknown }) => {
    const body = { type, title, status, ...rest, traceId }; // same property order as ASP.NET Core
    res.writeHead(status, { "Content-Type": "application/problem+json" }).end(JSON.stringify(body));
    console.log(`[api] #${no} ${status} problem+json`);
  };

  const invalid = items.findIndex((i) => !(i.quantity >= 1));
  if (items.length === 0 || invalid >= 0) {
    return problem(400, {
      type: "https://tools.ietf.org/html/rfc9110#section-15.5.1",
      title: "One or more validation errors occurred.",
      errors: { [`items[${Math.max(invalid, 0)}].quantity`]: ["Quantity must be at least 1."] },
    });
  }
  for (const item of items) {
    const book = books.find((b) => b.isbn === item.isbn);
    if (book && item.quantity > book.stock) {
      return problem(409, {
        type: "https://bookstore.example.com/problems/out-of-stock",
        title: "Not enough stock",
        detail: `Only ${book.stock} copies of ${book.title} are in stock.`,
      });
    }
  }
  const order = { id: nextOrderId++, status: "Pending" };
  res.writeHead(201, { "Content-Type": "application/json" }).end(JSON.stringify(order));
  console.log(`[api] #${no} 201 order ${order.id}`);
}

server.listen(port, () => console.log(`[api] listening on http://localhost:${port}`));
