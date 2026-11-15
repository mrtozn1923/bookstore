import http from "node:http";

// A carrier that accepts the connection and never answers
const carrier = http.createServer(() => {});
carrier.listen(52187, "127.0.0.1");

const started = performance.now();
try {
    await fetch("http://127.0.0.1:52187/v1/tracking/BK1001TR");
} catch (err) {
    const cause = (err as Error).cause as { name: string; code: string };
    console.log(`${(err as Error).message} after ${((performance.now() - started) / 1000).toFixed(1)} s: ${cause.name} ${cause.code}`);
} finally {
    carrier.closeAllConnections();
    carrier.close();
}
