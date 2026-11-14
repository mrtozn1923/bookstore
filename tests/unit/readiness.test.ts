import { it } from "node:test";
import assert from "node:assert/strict";
import type sql from "mssql";
import { createReadinessCheck } from "../../src/lifecycle/readiness.ts";

it("reports not-ready when SQL Server does not answer within a second", async (t) => {
    // Fake timers: the 1 s timeout passes in no time
    t.mock.timers.enable({ apis: ["setTimeout"] });
    // A fake pool whose query hangs until it is cancelled, like an unreachable server
    let cancel = (): void => {};
    const request = {
        query: () => new Promise((resolve, reject) => (cancel = () => reject(new Error("Canceled.")))),
        cancel: () => cancel(),
    };
    const pool = { request: () => request } as unknown as sql.ConnectionPool;

    const check = createReadinessCheck(pool, { shuttingDown: false });
    const pending = check();
    t.mock.timers.tick(1_000);

    assert.deepEqual(await pending, { status: "not-ready", checks: { sql: "Canceled." } });
});
