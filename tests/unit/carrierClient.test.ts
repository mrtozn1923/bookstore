import { afterEach, beforeEach, describe, it } from "node:test";
import assert from "node:assert/strict";
import { MockAgent } from "undici";
import { CarrierClient, CarrierUnavailableError, TrackingNotFoundError } from "../../src/integrations/carrierClient.ts";

const BASE = "http://carrier.test";
const tracking = { trackingNumber: "BK1001TR", status: "in_transit", events: [] };

describe("CarrierClient", () => {
    let agent: MockAgent;
    let retries: number[];
    let client: CarrierClient;

    beforeEach(() => {
        // No real network: requests that no interceptor matches fail instead of leaving the machine
        agent = new MockAgent();
        agent.disableNetConnect();
        retries = [];
        client = new CarrierClient({
            baseUrl: BASE,
            apiKey: "fake-carrier-key-for-tests",
            timeoutMs: 200,
            attempts: 3,
            dispatcher: agent,
            onRetry: ({ delayMs }) => retries.push(delayMs),
        });
    });

    afterEach(async () => {
        agent.assertNoPendingInterceptors();
        await agent.close();
    });

    it("retries a 503 and honours Retry-After", async () => {
        const carrier = agent.get(BASE);
        carrier.intercept({ path: "/v1/tracking/BK1001TR" }).reply(503, "", { headers: { "retry-after": "0" } });
        carrier.intercept({ path: "/v1/tracking/BK1001TR", headers: { "x-api-key": "fake-carrier-key-for-tests" } }).reply(200, tracking);

        assert.deepEqual(await client.getTracking("BK1001TR"), tracking);
        assert.deepEqual(retries, [0]);
    });

    it("does not retry a 404", async () => {
        agent.get(BASE).intercept({ path: "/v1/tracking/BK0000TR" }).reply(404, { error: "unknown" });

        await assert.rejects(client.getTracking("BK0000TR"), TrackingNotFoundError);
        assert.deepEqual(retries, []);
    });

    it("gives up after three slow answers", async () => {
        agent.get(BASE).intercept({ path: "/v1/tracking/BK1001TR" }).reply(200, tracking).delay(500).times(3);

        await assert.rejects(client.getTracking("BK1001TR"), (err: unknown) => err instanceof CarrierUnavailableError && err.timedOut);
        assert.equal(retries.length, 2);
    });

    it("stops retrying when the caller aborts", async () => {
        agent.get(BASE).intercept({ path: "/v1/tracking/BK1001TR" }).reply(503, "").persist();
        const caller = new AbortController();
        setTimeout(() => caller.abort(new Error("client went away")), 50);

        await assert.rejects(client.getTracking("BK1001TR", caller.signal), /client went away/);
    });
});
