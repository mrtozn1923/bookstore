#!/usr/bin/env node
// Back office CLI, registered in package.json "bin": npx bookstore-tools <command>
import { main } from "../src/tools/cli.ts";

// Ctrl+C and SIGTERM cancel the running work through a signal instead of killing the process mid-write;
// a second Ctrl+C has no listener any more and stops the process at once
const interrupted = new AbortController();
for (const name of ["SIGINT", "SIGTERM"] as const) {
    process.once(name, () => interrupted.abort(new Error(`received ${name}`)));
}

// exitCode instead of process.exit(): pending writes to stdout and files still finish
process.exitCode = await main(process.argv.slice(2), {
    stdout: process.stdout,
    stderr: process.stderr,
    stdin: process.stdin,
    signal: interrupted.signal,
});
