import { it } from "node:test";
import assert from "node:assert/strict";
import request from "supertest";
import { createTestApp, WEBHOOK_SECRET } from "../helpers.ts";
import { SIGNATURE_HEADER, signPayload } from "../../src/security/webhookSignature.ts";

const event = { id: "pay-evt-1001", type: "payment.succeeded", orderId: "1001", amountMinor: 34990, currency: "TRY" };
// The provider's own formatting: spaces after the colons
const body = JSON.stringify(event, null, 1);
const now = () => Math.floor(Date.now() / 1000);

function post(app: Parameters<typeof request>[0], signature: string, payload = body) {
    return request(app).post("/api/payments/webhook").set("content-type", "application/json").set(SIGNATURE_HEADER, signature).send(payload);
}

it("accepts a signed event once and reports the retry as a duplicate", async () => {
    const { app } = createTestApp();
    const signature = signPayload(Buffer.from(body), WEBHOOK_SECRET, now());
    assert.deepEqual((await post(app, signature).expect(200)).body, { received: true, duplicate: false });
    assert.deepEqual((await post(app, signature).expect(200)).body, { received: true, duplicate: true });
});

it("rejects a changed body, a wrong secret and an old timestamp", async () => {
    const { app } = createTestApp();
    const signature = signPayload(Buffer.from(body), WEBHOOK_SECRET, now());
    await post(app, signature, body.replace("34990", "1")).expect(401);
    await post(app, signPayload(Buffer.from(body), "another-secret-not-real", now())).expect(401);
    await post(app, signPayload(Buffer.from(body), WEBHOOK_SECRET, now() - 600)).expect(401);
    await post(app, "sig=whatever").expect(401);
});
