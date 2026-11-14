import { createServer } from "node:http";
import { connect } from "node:net";

// Answers 413 without reading the body; with "close" it also asks to close the connection
const server = createServer((req, res) => {
    res.writeHead(413, process.argv[2] === "close" ? { connection: "close" } : {});
    res.end("too large");
});
await new Promise<void>((resolve) => server.listen(0, "127.0.0.1", resolve));
const { port } = server.address() as { port: number };

// The client announces 100,000 bytes but sends only 1,000 of them
const socket = connect(port, "127.0.0.1", () => {
    socket.write("POST /api/orders HTTP/1.1\r\nHost: localhost\r\nContent-Type: application/json\r\nContent-Length: 100000\r\n\r\n" + "x".repeat(1000));
});
const check = setTimeout(() => {
    console.log("connection still open after 1 s, the server waits for the rest of the body");
    socket.destroy();
}, 1000);
socket.on("data", (data) => console.log(data.toString().split("\r\n").slice(0, 3).join(" | ")));
socket.on("close", () => {
    clearTimeout(check);
    console.log("connection closed");
    server.close();
});
