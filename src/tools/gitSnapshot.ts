import { execFile, spawn } from "node:child_process";
import { createWriteStream } from "node:fs";
import { rm, stat } from "node:fs/promises";
import { once } from "node:events";
import { pipeline } from "node:stream/promises";
import { promisify } from "node:util";

const execFileAsync = promisify(execFile);

// Arguments go as an array and no shell is started: nothing in them is interpreted
async function git(args: string[], signal?: AbortSignal): Promise<string> {
    const { stdout } = await execFileAsync("git", args, { signal, maxBuffer: 1024 * 1024 });
    return stdout.trim();
}

export interface GitSnapshot {
    file: string;
    commit: string;
    bytes: number;
}

// Zips the committed source at HEAD; git writes the zip, so it works the same on Windows, Linux and macOS
export async function createGitSnapshot(options: { out?: string; allowDirty: boolean; signal?: AbortSignal }): Promise<GitSnapshot> {
    const commit = await git(["rev-parse", "--short", "HEAD"], options.signal);
    const changes = await git(["status", "--porcelain"], options.signal);
    if (changes && !options.allowDirty) {
        throw new Error(`the working tree has ${changes.split("\n").length} uncommitted change(s); commit them or pass --allow-dirty`);
    }
    const file = options.out ?? `bookstore-api-${commit}.zip`;

    // git archive writes the zip to stdout; it is streamed to the file instead of being buffered in memory
    const child = spawn("git", ["archive", "--format=zip", "HEAD"], { signal: options.signal, stdio: ["ignore", "pipe", "pipe"] });
    let stderr = "";
    child.stderr.setEncoding("utf8").on("data", (chunk: string) => (stderr += chunk));
    try {
        const [, [code, signal]] = await Promise.all([pipeline(child.stdout, createWriteStream(file)), once(child, "close")]);
        if (code !== 0) {
            throw new Error(`git archive failed (${signal ?? `exit code ${code}`}): ${stderr.trim()}`);
        }
    } catch (err) {
        // Never leave half a zip behind
        await rm(file, { force: true });
        throw err;
    }
    return { file, commit, bytes: (await stat(file)).size };
}
