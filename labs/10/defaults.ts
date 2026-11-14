import { createServer, maxHeaderSize } from "node:http";

const server = createServer();
console.log({
    headersTimeout: server.headersTimeout,
    requestTimeout: server.requestTimeout,
    keepAliveTimeout: server.keepAliveTimeout,
    timeout: server.timeout,
    maxRequestsPerSocket: server.maxRequestsPerSocket,
    maxHeaderSize,
});
server.listen(0, () => {
    console.log("listen() without a host:", server.address());
    server.close();
});
