import { createWriteStream } from "node:fs";
import { mkdir, rename, rm } from "node:fs/promises";
import { finished } from "node:stream/promises";
import { setTimeout as delay } from "node:timers/promises";

// An export that takes a few seconds; run with --handle to clean up on Ctrl+C / SIGTERM
const handle = process.argv.includes("--handle");
await mkdir("labs/19/out", { recursive: true });
const temp = "labs/19/out/export.csv.tmp";
const target = "labs/19/out/export.csv";

const stop = new AbortController();
if (handle) {
    for (const name of ["SIGINT", "SIGTERM"] as const) {
        process.once(name, () => stop.abort(new Error(`received ${name}`)));
    }
}

const out = createWriteStream(temp);
let lines = 0;
try {
    for (let batch = 0; batch < 50; batch++) {
        for (let i = 0; i < 1_000; i++) {
            out.write(`${++lines};9780441172719;1;349.90\n`);
        }
        // The wait stands in for reading the next batch from the database
        await delay(100, undefined, { signal: stop.signal });
    }
    out.end();
    await finished(out);
    await rename(temp, target);
    console.log(`export finished: ${lines} lines`);
} catch (err) {
    out.destroy();
    await rm(temp, { force: true });
    console.error(`export cancelled after ${lines} lines (${(stop.signal.reason as Error).message}), temp file removed`);
    process.exitCode = 130;
}
