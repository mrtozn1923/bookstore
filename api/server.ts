// A tiny mock of Bookstore.Api for the RxJS series: no framework, no database.
// Run: node api/server.ts   (PORT defaults to 5180)
import { createServer } from "node:http";
import { books, orders } from "../data/books.ts";

const port = Number(process.env.PORT ?? 5180);
const delayMs = Number(process.env.DELAY_MS ?? 200);
const failures = new Map<string, number>();
const statusPolls = new Map<number, number>();
let requestNo = 0;

const server = createServer((req, res) => {
  const no = ++requestNo;
  const url = new URL(req.url ?? "/", `http://localhost:${port}`);
  console.log(`[api] #${no} ${req.method} ${url.pathname}${url.search}`);

  res.on("close", () => {
    if (!res.writableFinished) console.log(`[api] #${no} client aborted`);
  });

  setTimeout(() => {
    if (res.destroyed) return;
    res.setHeader("Access-Control-Allow-Origin", "*");

    // ?fail=N: the first N requests to this path answer 503
    const fail = Number(url.searchParams.get("fail") ?? 0);
    const seen = failures.get(url.pathname) ?? 0;
    if (seen < fail) {
      failures.set(url.pathname, seen + 1);
      res.writeHead(503).end();
      console.log(`[api] #${no} 503`);
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

    const match = url.pathname.match(/^\/api\/books(?:\/(\d{13}))?(?:\/(stock|price))?$/);
    const book = match?.[1] ? books.find((b) => b.isbn === match[1]) : undefined;
    let body: unknown;
    if (match && !match[1]) body = books;
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

server.listen(port, () => console.log(`[api] listening on http://localhost:${port}`));
