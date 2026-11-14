// A tiny stand-in for the Bookstore API: a fast endpoint, an I/O-bound one and a CPU-bound one
import { createServer } from "node:http";
import { setTimeout as delay } from "node:timers/promises";

const books = [
    { isbn: "9780441172719", title: "Dune", price: 349.9 },
    { isbn: "9780547928227", title: "The Hobbit", price: 289.5 },
    { isbn: "9780141439600", title: "Middlemarch", price: 310.0 },
];

function json(res, status, body) {
    res.writeHead(status, { "content-type": "application/json" });
    res.end(JSON.stringify(body));
}

const server = createServer(async (req, res) => {
    if (req.url === "/api/books") {
        return json(res, 200, books);
    }
    if (req.url === "/api/books/from-db") {
        // Simulates a database query: 100 ms of waiting on I/O, the thread stays free
        await delay(100);
        return json(res, 200, books);
    }
    if (req.url === "/api/reports/price-summary") {
        // CPU-bound work: a fake price report over 20 million rows
        const started = performance.now();
        let total = 0;
        for (let i = 0; i < 20_000_000; i++) {
            total += Math.sqrt(i) * 1.18;
            total = Number(total.toFixed(2));
        }
        return json(res, 200, { total, ms: Math.round(performance.now() - started) });
    }
    json(res, 404, { error: "not found" });
});

server.listen(3000, () => console.log("listening on http://localhost:3000"));
