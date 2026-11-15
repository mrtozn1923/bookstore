import { access, mkdir } from "node:fs/promises";
import path from "node:path";
import { createInterface } from "node:readline/promises";
import { parseArgs, styleText } from "node:util";
import { readCatalogCsv } from "#services/catalogImport";
import { writeFileAtomic } from "#storage/atomicWrite";
import { createGitSnapshot } from "./gitSnapshot.ts";

// 0 success, 1 the work failed, 2 wrong usage, 130 = 128 + SIGINT (what shells report after Ctrl+C)
export const EXIT = { ok: 0, failed: 1, usage: 2, interrupted: 130 } as const;

export interface CliIo {
    stdout: NodeJS.WritableStream;
    stderr: NodeJS.WritableStream;
    stdin: NodeJS.ReadableStream & { isTTY?: boolean };
    signal: AbortSignal;
}

class UsageError extends Error {}

const USAGE = `usage: bookstore-tools <command> [options]
  import-books <file.csv> [--out <file.json>] [--dry-run] [--json] [--yes]
  snapshot [--out <file.zip>] [--allow-dirty]`;

export async function main(argv: string[], io: CliIo): Promise<number> {
    // styleText checks the stream: colours only on a terminal, never with NO_COLOR or into a file
    const red = (text: string) => styleText("red", text, { stream: io.stderr });
    const [command, ...args] = argv;
    try {
        switch (command) {
            case "import-books":
                return await importBooks(args, io);
            case "snapshot":
                return await snapshot(args, io);
            case "help":
                io.stdout.write(`${USAGE}\n`);
                return EXIT.ok;
            default:
                throw new UsageError(command ? `unknown command '${command}'` : "no command given");
        }
    } catch (err) {
        if (io.signal.aborted) {
            io.stderr.write(`${red("interrupted:")} ${(io.signal.reason as Error).message}\n`);
            return EXIT.interrupted;
        }
        const code = (err as { code?: string }).code ?? "";
        if (err instanceof UsageError || code.startsWith("ERR_PARSE_ARGS_")) {
            io.stderr.write(`${red("error:")} ${(err as Error).message}\n${USAGE}\n`);
            return EXIT.usage;
        }
        io.stderr.write(`${red("error:")} ${(err as Error).message}\n`);
        return EXIT.failed;
    }
}

async function importBooks(args: string[], io: CliIo): Promise<number> {
    const { values, positionals } = parseArgs({
        args,
        allowPositionals: true,
        options: {
            out: { type: "string", short: "o", default: "data/catalog.json" },
            "dry-run": { type: "boolean", default: false },
            json: { type: "boolean", default: false },
            yes: { type: "boolean", short: "y", default: false },
        },
    });
    if (positionals.length !== 1) {
        throw new UsageError("import-books needs exactly one CSV file");
    }
    const [csvPath] = positionals;
    const { books, errors } = await readCatalogCsv(csvPath);

    // Problems go to stderr, the result to stdout: "--json > result.json" stays valid JSON
    for (const error of errors) {
        io.stderr.write(`${styleText("yellow", "skipped", { stream: io.stderr })} ${error}\n`);
    }
    const dryRun = values["dry-run"];
    if (!dryRun) {
        if ((await exists(values.out)) && !values.yes && !(await confirm(`${values.out} exists. Overwrite? (y/N) `, io))) {
            io.stderr.write("nothing written\n");
            return EXIT.failed;
        }
        await mkdir(path.dirname(values.out), { recursive: true });
        await writeFileAtomic(values.out, JSON.stringify(books, null, 2) + "\n");
    }

    if (values.json) {
        io.stdout.write(JSON.stringify({ file: csvPath, imported: books.length, skipped: errors.length, written: dryRun ? null : values.out }) + "\n");
    } else {
        const target = dryRun ? "would be imported (dry run)" : `imported into ${values.out}`;
        io.stdout.write(`${styleText("green", "done", { stream: io.stdout })} ${books.length} books ${target}, ${errors.length} rows skipped\n`);
    }
    return errors.length > 0 ? EXIT.failed : EXIT.ok;
}

async function snapshot(args: string[], io: CliIo): Promise<number> {
    const { values } = parseArgs({
        args,
        options: { out: { type: "string", short: "o" }, "allow-dirty": { type: "boolean", default: false } },
    });
    const result = await createGitSnapshot({ out: values.out, allowDirty: values["allow-dirty"], signal: io.signal });
    io.stdout.write(`${styleText("green", "done", { stream: io.stdout })} ${result.file} (commit ${result.commit}, ${Math.round(result.bytes / 1024)} KB)\n`);
    return EXIT.ok;
}

async function exists(file: string): Promise<boolean> {
    return access(file).then(
        () => true,
        () => false,
    );
}

// Asks only on a terminal; in CI or behind a pipe nobody can answer, so the answer is "no"
async function confirm(question: string, io: CliIo): Promise<boolean> {
    if (!io.stdin.isTTY) {
        io.stderr.write(`${question.trim()} no terminal to ask, pass --yes\n`);
        return false;
    }
    const rl = createInterface({ input: io.stdin, output: io.stderr });
    try {
        const answer = await rl.question(question, { signal: io.signal });
        return /^y(es)?$/i.test(answer.trim());
    } finally {
        rl.close();
    }
}
