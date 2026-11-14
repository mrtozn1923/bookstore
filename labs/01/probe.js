// Sends requests to server.js and measures how long they take
const base = "http://localhost:3000";

async function timed(path) {
    const started = performance.now();
    const res = await fetch(base + path);
    await res.json();
    return (performance.now() - started).toFixed(1);
}

// 1) 500 concurrent "database" requests
let started = performance.now();
await Promise.all(Array.from({ length: 500 }, () => timed("/api/books/from-db")));
console.log(`500 concurrent /api/books/from-db: ${Math.round(performance.now() - started)} ms total`);

// 2) /api/books while the report request is running
const report = timed("/api/reports/price-summary").then((ms) => console.log(`price-summary: ${ms} ms`));
await new Promise((r) => setTimeout(r, 50));
const fast = await Promise.all([timed("/api/books"), timed("/api/books"), timed("/api/books")]);
console.log(`/api/books while the report runs: ${fast.join(" ms, ")} ms`);
await report;
console.log(`/api/books after the report: ${await timed("/api/books")} ms`);
