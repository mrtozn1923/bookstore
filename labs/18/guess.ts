import http from "node:http";

// The carrier answers 404 for a tracking number it does not know
const carrier = http.createServer((req, res) => {
    res.writeHead(404, { "content-type": "application/json" }).end(JSON.stringify({ error: "unknown tracking number" }));
});
carrier.listen(52180, "127.0.0.1");

try {
    const response = await fetch("http://127.0.0.1:52180/v1/tracking/BK9999TR");
    const tracking = await response.json();
    console.log("tracking:", tracking);
} catch (err) {
    console.log("caught:", (err as Error).message);
} finally {
    carrier.close();
}
