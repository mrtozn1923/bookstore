import { createServer } from "node:http";
import { connect } from "node:net";

// headersTimeout lowered to 2 s so the demo is short; the check runs every 500 ms
const server = createServer({ headersTimeout: 2_000, connectionsCheckingInterval: 500 }, (req, res) => res.end("ok"));
await new Promise<void>((resolve) => server.listen(0, "127.0.0.1", resolve));
const { port } = server.address() as { port: number };

// A slow client: sends the request line, then one header byte every 300 ms and never finishes
const started = performance.now();
const socket = connect(port, "127.0.0.1", () => socket.write("GET /api/books HTTP/1.1\r\nHost: localhost\r\n"));
const drip = setInterval(() => socket.write("X"), 300);
socket.on("data", (data) => console.log(`after ${Math.round(performance.now() - started)} ms the server sent: ${data.toString().split("\r\n")[0]}`));
socket.on("close", () => {
    console.log(`connection closed after ${Math.round(performance.now() - started)} ms`);
    clearInterval(drip);
    server.close();
});
