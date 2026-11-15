import { after, describe, it } from "node:test";
import assert from "node:assert/strict";
import { createCatalogParsePool } from "#services/catalogParsePool";

const HEADER = "isbn;title;author;price;currency\n";

describe("catalog parse pool (real worker threads)", () => {
    const pool = createCatalogParsePool({ maxThreads: 2 });
    after(() => pool.close());

    it("parses on a worker and moves the bytes there", async () => {
        const csv = Buffer.from(HEADER + "9780441172719;Dune;Frank Herbert;349,90;TRY\n" + "x".repeat(70_000));
        const result = await pool.parse(csv);
        assert.equal(result.ok, true);
        // The Buffer owned its memory, so it was transferred, not copied
        assert.equal(csv.byteLength, 0);
    });

    it("returns a format error as data", async () => {
        const result = await pool.parse(Buffer.from("id;name\n1;Dune\n"));
        assert.deepEqual(result, { ok: false, reason: "unexpected header: id;name" });
    });

    it("stops a running task when the caller aborts", async () => {
        const csv = Buffer.from(HEADER + "9780441172719;Dune;Frank Herbert;349,90;TRY\n".repeat(400_000));
        await assert.rejects(pool.parse(csv, AbortSignal.timeout(20)), { name: "AbortError" });
    });
});
