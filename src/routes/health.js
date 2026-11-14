// GET /health: is the process up, and which Node version is it running on?
export function handleHealth(req, res) {
    const body = {
        status: "ok",
        service: "bookstore-api",
        node: process.version,
        uptimeSeconds: Math.round(process.uptime()),
    };
    res.writeHead(200, { "content-type": "application/json" });
    res.end(JSON.stringify(body));
}
