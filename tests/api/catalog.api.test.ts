import { it } from "node:test";
import assert from "node:assert/strict";
import request from "supertest";
import { createTestApp } from "../helpers.ts";

const csv = "isbn;title;author;price;currency\n9780441172719;Dune;Frank Herbert;349,90;TRY\n9780000000000;Bad;Nobody;1,00;TRY\n";

it("validates a catalog upload for store staff", async () => {
    const { app } = createTestApp();
    const res = await request(app).post("/api/catalog/validate").set("authorization", "Bearer token-grace").set("content-type", "text/csv").send(csv).expect(200);
    assert.equal(res.body.imported, 1);
    assert.deepEqual(res.body.errors, ["line 3: invalid ISBN-13 9780000000000"]);
});

it("answers 400 for a wrong header and 403 for customers", async () => {
    const { app } = createTestApp();
    await request(app).post("/api/catalog/validate").set("authorization", "Bearer token-grace").set("content-type", "text/csv").send("id;name\n").expect(400);
    await request(app).post("/api/catalog/validate").set("authorization", "Bearer token-ada").set("content-type", "text/csv").send(csv).expect(403);
});
