import { it } from "node:test";
import assert from "node:assert/strict";
import request from "supertest";
import { createTestApp } from "../helpers.ts";
import { CarrierUnavailableError } from "../../src/integrations/carrierClient.ts";

const grace = { authorization: "Bearer token-grace" };

it("returns the carrier's tracking for store staff", async () => {
    const { app } = createTestApp();
    const res = await request(app).get("/api/shipments/BK1001TR").set(grace).expect(200);
    assert.equal(res.body.status, "in_transit");
});

it("is only for store staff", async () => {
    const { app } = createTestApp();
    await request(app).get("/api/shipments/BK1001TR").set({ authorization: "Bearer token-ada" }).expect(403);
});

it("maps the carrier's 404 and its timeouts", async () => {
    const { app } = createTestApp();
    await request(app).get("/api/shipments/BK0000TR").set(grace).expect(404);

    const slow = { getTracking: async () => Promise.reject(new CarrierUnavailableError("carrier did not answer", { timedOut: true })) };
    const res = await request(createTestApp(undefined, {}, slow).app).get("/api/shipments/BK1001TR").set(grace).expect(504);
    assert.equal(res.body.title, "Gateway Timeout");
});
