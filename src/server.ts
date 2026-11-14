import { createApp } from "./app.ts";
import { createServices } from "./composition.ts";

const port: number = Number(process.env.PORT ?? 3000);
// Loopback only by default; containers set HOST=0.0.0.0
const host: string = process.env.HOST ?? "127.0.0.1";

const app = createApp(createServices());
// Express 5 passes listen errors (EADDRINUSE...) to this callback instead of throwing them
const server = app.listen(port, host, (err?: Error) => {
    if (err) {
        console.error(`cannot listen on ${host}:${port}: ${err.message}`);
        process.exit(1);
    }
    console.log(`Bookstore API listening on http://${host}:${port} (${app.get("env")})`);
});

// app.listen returns the node:http server, so the timeouts from post 10 still apply here
server.headersTimeout = 10_000;
server.requestTimeout = 30_000;
server.keepAliveTimeout = 5_000;
