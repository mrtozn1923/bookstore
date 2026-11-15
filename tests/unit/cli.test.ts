import { after, before, describe, it } from "node:test";
import assert from "node:assert/strict";
import { mkdtemp, readFile, rm, writeFile } from "node:fs/promises";
import { tmpdir } from "node:os";
import path from "node:path";
import { PassThrough } from "node:stream";
import { EXIT, main } from "../../src/tools/cli.ts";

// Runs the CLI in-process with captured streams: no child process, no terminal
async function run(argv: string[]) {
    const stdout = new PassThrough();
    const stderr = new PassThrough();
    let out = "";
    let err = "";
    stdout.on("data", (chunk) => (out += chunk));
    stderr.on("data", (chunk) => (err += chunk));
    const code = await main(argv, { stdout, stderr, stdin: new PassThrough(), signal: new AbortController().signal });
    return { code, out, err };
}

describe("bookstore-tools", () => {
    let dir: string;
    let csv: string;

    before(async () => {
        dir = await mkdtemp(path.join(tmpdir(), "bookstore-tools-"));
        csv = path.join(dir, "books.csv");
        await writeFile(csv, "isbn;title;author;price;currency\n9780441172719;Dune;Frank Herbert;349,90;TRY\n");
    });
    after(() => rm(dir, { recursive: true, force: true }));

    it("rejects an unknown option with exit code 2", async () => {
        const { code, err } = await run(["import-books", "--dryrun", csv]);
        assert.equal(code, EXIT.usage);
        assert.match(err, /Unknown option '--dryrun'/);
    });

    it("prints only JSON on stdout with --json", async () => {
        const { code, out } = await run(["import-books", csv, "--dry-run", "--json"]);
        assert.equal(code, EXIT.ok);
        assert.deepEqual(JSON.parse(out), { file: csv, imported: 1, skipped: 0, written: null });
    });

    it("does not overwrite without a terminal or --yes", async () => {
        const target = path.join(dir, "catalog.json");
        await writeFile(target, "[]\n");

        const refused = await run(["import-books", csv, "--out", target]);
        assert.equal(refused.code, EXIT.failed);
        assert.equal(await readFile(target, "utf8"), "[]\n");

        const forced = await run(["import-books", csv, "--out", target, "--yes"]);
        assert.equal(forced.code, EXIT.ok);
        assert.equal(JSON.parse(await readFile(target, "utf8")).length, 1);
    });
});
