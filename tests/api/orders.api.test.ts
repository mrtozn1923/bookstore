import { describe, it } from "node:test";
import assert from "node:assert/strict";
import request from "supertest";
import { createTestApp, DUNE, EMMA } from "../helpers.ts";

// supertest calls the Express app in memory: no port, no SQL Server (the WebApplicationFactory counterpart)
describe("POST /api/orders", () => {
    it("401 without a token", async () => {
        const { app } = createTestApp();
        const res = await request(app).post("/api/orders").send({ lines: [{ isbn: DUNE, quantity: 1 }] });
        assert.equal(res.status, 401);
        assert.equal(res.headers["www-authenticate"], "Bearer");
    });

    it("201 with Location, the customer taken from the token", async () => {
        const { app } = createTestApp();
        const res = await request(app)
            .post("/api/orders")
            .set("authorization", "Bearer token-ada")
            .send({ lines: [{ isbn: DUNE, quantity: 3 }, { isbn: EMMA, quantity: 1 }] })
            .expect("content-type", /json/)
            .expect(201);
        assert.equal(res.headers.location, `/api/orders/${res.body.id}`);
        assert.equal(res.body.customerId, "ada");
        assert.deepEqual(res.body.total, { amount: 1294.7, currency: "TRY" });
    });

    it("409 when the stock is not enough", async () => {
        const { app } = createTestApp({ [DUNE]: 1 });
        const res = await request(app).post("/api/orders").set("authorization", "Bearer token-ada").send({ lines: [{ isbn: DUNE, quantity: 2 }] });
        assert.equal(res.status, 409);
        assert.equal(res.body.detail, `not enough stock for ISBN: ${DUNE}`);
    });

    it("400 with every validation error in one Problem Details body", async (t) => {
        const { app } = createTestApp();
        const res = await request(app)
            .post("/api/orders")
            .set("authorization", "Bearer token-ada")
            .set("x-request-id", "req-16-snapshot")
            .send({ customerId: "alan", lines: [{ isbn: "978-0441172719", quantity: 0 }] });
        assert.equal(res.status, 400);
        // The first run with --test-update-snapshots writes the file; later runs compare against it
        t.assert.snapshot(res.body);
    });
});

describe("GET /api/orders/:id", () => {
    it("404 for someone else's order, 200 for the owner and for store staff", async () => {
        const { app } = createTestApp();
        const placed = await request(app).post("/api/orders").set("authorization", "Bearer token-ada").send({ lines: [{ isbn: DUNE, quantity: 1 }] });

        const statusFor = async (token: string) => (await request(app).get(placed.headers.location).set("authorization", `Bearer ${token}`)).status;
        assert.equal(await statusFor("token-ada"), 200);
        assert.equal(await statusFor("token-alan"), 404);
        assert.equal(await statusFor("token-grace"), 200);
        assert.equal(await statusFor("token-unknown"), 401);
    });
});

describe("rate limiting", () => {
    it("429 after the limit, as Problem Details", async () => {
        const { app } = createTestApp(undefined, { rateLimitPerMinute: 2 });
        const statuses: number[] = [];
        for (let i = 0; i < 3; i++) {
            statuses.push((await request(app).get("/api/books")).status);
        }
        assert.deepEqual(statuses, [200, 200, 429]);
    });
});
