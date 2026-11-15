// Development-only stand-in for the carrier's tracking API: node scripts/dev-carrier.ts
// CARRIER_DELAY_MS slows every answer down; CARRIER_FAIL_EVERY=n lets only every n-th request through, the rest get 503
import http from "node:http";
import { setTimeout as delay } from "node:timers/promises";

const port = Number(process.env.DEV_CARRIER_PORT ?? 4002);
const delayMs = Number(process.env.CARRIER_DELAY_MS ?? 0);
const failEvery = Number(process.env.CARRIER_FAIL_EVERY ?? 0);

const tracking = new Map([
    ["BK1001TR", { status: "in_transit", events: [["2026-11-14T09:12:00Z", "label_created", "Istanbul"], ["2026-11-14T18:40:00Z", "in_transit", "Istanbul hub"]] }],
    ["BK1002TR", { status: "delivered", events: [["2026-11-12T10:05:00Z", "in_transit", "Ankara hub"], ["2026-11-13T14:22:00Z", "delivered", "Ankara"]] }],
]);
// Idempotency-Key -> shipment: the same key never creates a second shipment
const shipments = new Map<string, { trackingNumber: string; orderId: string }>();
let requests = 0;

function send(res: http.ServerResponse, status: number, body: unknown, headers: Record<string, string> = {}): void {
    res.writeHead(status, { "content-type": "application/json", ...headers }).end(JSON.stringify(body));
}

async function readJson(req: http.IncomingMessage): Promise<{ orderId?: string }> {
    let text = "";
    for await (const chunk of req) {
        text += chunk;
    }
    return JSON.parse(text || "{}");
}

http.createServer(async (req, res) => {
    requests++;
    const n = requests;
    console.log(`#${n} ${req.method} ${req.url} key=${req.headers["idempotency-key"] ?? "-"}`);
    if (!req.headers["x-api-key"]) {
        return send(res, 401, { error: "missing x-api-key" });
    }
    const body = req.method === "POST" ? await readJson(req) : {};
    if (delayMs > 0) {
        await delay(delayMs);
    }
    if (failEvery > 0 && n % failEvery !== 0) {
        return send(res, 503, { error: "busy" }, { "retry-after": "1" });
    }
    const match = /^\/v1\/tracking\/([A-Z0-9]+)$/.exec(req.url ?? "");
    if (req.method === "GET" && match) {
        const item = tracking.get(match[1]);
        if (!item) {
            return send(res, 404, { error: "unknown tracking number" });
        }
        const events = item.events.map(([at, status, location]) => ({ at, status, location }));
        return send(res, 200, { trackingNumber: match[1], status: item.status, events });
    }
    if (req.method === "POST" && req.url === "/v1/shipments") {
        const key = String(req.headers["idempotency-key"] ?? "");
        let shipment = key ? shipments.get(key) : undefined;
        if (!shipment) {
            shipment = { trackingNumber: `BK${1000 + shipments.size + 3}TR`, orderId: String(body.orderId) };
            if (key) {
                shipments.set(key, shipment);
            }
            console.log(`#${n} created shipment ${shipment.trackingNumber} for order ${shipment.orderId}`);
        }
        return send(res, 201, shipment);
    }
    send(res, 404, { error: "not found" });
}).listen(port, "127.0.0.1", () => console.log(`dev carrier on http://127.0.0.1:${port}`));
