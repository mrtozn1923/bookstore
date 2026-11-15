import http from "node:http";
import { setTimeout as delay } from "node:timers/promises";
import { withRetry } from "../../src/integrations/retry.ts";
import { CarrierClient, CarrierUnavailableError } from "../../src/integrations/carrierClient.ts";

// The carrier creates the shipment, but its first answer of each order arrives after 2.5 s
const byKey = new Map<string, string>();
const seenOrders = new Set<string>();
let created = 0;
const carrier = http.createServer(async (req, res) => {
    let text = "";
    for await (const chunk of req) {
        text += chunk;
    }
    const { orderId } = JSON.parse(text) as { orderId: string };
    const key = req.headers["idempotency-key"] as string | undefined;
    let trackingNumber = key ? byKey.get(key) : undefined;
    if (!trackingNumber) {
        trackingNumber = `BK${2000 + ++created}TR`;
        if (key) {
            byKey.set(key, trackingNumber);
        }
        console.log(`  carrier: created ${trackingNumber} for order ${orderId} (key ${key ? key.slice(0, 8) : "none"})`);
    }
    if (!seenOrders.has(orderId)) {
        seenOrders.add(orderId);
        await delay(2_500);
    }
    res.writeHead(201, { "content-type": "application/json" }).end(JSON.stringify({ trackingNumber, orderId }));
});
carrier.listen(52184, "127.0.0.1");
const baseUrl = "http://127.0.0.1:52184";

console.log("naive retry, no key:");
const naive = await withRetry(
    async () => {
        try {
            const response = await fetch(`${baseUrl}/v1/shipments`, {
                method: "POST",
                headers: { "content-type": "application/json" },
                body: JSON.stringify({ orderId: "1001" }),
                signal: AbortSignal.timeout(2_000),
            });
            return (await response.json()) as { trackingNumber: string };
        } catch (err) {
            throw new CarrierUnavailableError("timeout", { timedOut: true, cause: err });
        }
    },
    { attempts: 3, baseDelayMs: 200, maxDelayMs: 2_000, isRetryable: (err) => err instanceof CarrierUnavailableError },
);
console.log(`  client got ${naive.trackingNumber}`);

console.log("CarrierClient, one Idempotency-Key per operation:");
const client = new CarrierClient({
    baseUrl,
    apiKey: "fake-carrier-key-for-tests",
    timeoutMs: 2_000,
    attempts: 3,
    onRetry: ({ attempt, delayMs, err }) => console.log(`  client: attempt ${attempt} failed (${(err as Error).message}), retrying in ${delayMs} ms`),
});
const shipment = await client.createShipment("1002");
console.log(`  client got ${shipment.trackingNumber}`);
console.log(`shipments created at the carrier: ${created}`);

carrier.closeAllConnections();
carrier.close();
