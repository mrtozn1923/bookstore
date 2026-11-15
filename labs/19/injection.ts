import { exec, execFile } from "node:child_process";
import { promisify } from "node:util";

const execAsync = promisify(exec);
const execFileAsync = promisify(execFile);

// A branch name that came from user input (a CLI argument, a web form...)
const branch = "main; echo INJECTED: this line was run by the shell";

// exec: one string, handed to a shell (/bin/sh, cmd.exe on Windows); ';' starts a second command
const viaShell = await execAsync(`git log --oneline -1 ${branch}`);
console.log("exec:\n" + viaShell.stdout.trim());

// execFile: no shell; the whole value is a single argument for git
try {
    await execFileAsync("git", ["log", "--oneline", "-1", branch]);
} catch (err) {
    console.log("execFile:\n" + (err as { stderr: string }).stderr.trim().split("\n")[0]);
}
