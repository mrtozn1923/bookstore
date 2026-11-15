// Does the CLI scan obj/? Repo copy vs. a copy outside git, with and without a .gitignore.
import { execFileSync } from "node:child_process";
import { cpSync, mkdtempSync, readFileSync, rmSync, writeFileSync, symlinkSync } from "node:fs";
import { tmpdir } from "node:os";
import { join, resolve } from "node:path";

const web = resolve("../aspnet/Bookstore.Web");
const cli = join(web, "node_modules/.bin/tailwindcss");
const probe = '<p class="bg-lime-300">probe</p>\n';
const build = (dir) => {
    execFileSync(cli, ["-i", "Styles/input.css", "-o", "out.css"], { cwd: dir, stdio: "ignore" });
    const hit = readFileSync(join(dir, "out.css"), "utf8").includes(".bg-lime-300");
    rmSync(join(dir, "out.css"));
    return hit ? 1 : 0;
};

// 1) inside the repo: app/.gitignore lists obj/
writeFileSync(join(web, "obj/probe.html"), probe);
console.log(`in repo: lime=${build(web)}`);
rmSync(join(web, "obj/probe.html"));

// 2) the same project copied outside any git repository
const tmp = mkdtempSync(join(tmpdir(), "tw-scan-"));
cpSync(web, tmp, { recursive: true, filter: (src) => !src.includes("node_modules") && !src.endsWith(".gitignore") });
symlinkSync(join(web, "node_modules"), join(tmp, "node_modules"));
writeFileSync(join(tmp, "obj/probe.html"), probe);
console.log(`no git: lime=${build(tmp)}`);

// 3) outside git, with the .gitignore that `dotnet new gitignore` writes
execFileSync("dotnet", ["new", "gitignore", "-o", tmp], { stdio: "ignore" });
console.log(`no git + dotnet new gitignore: lime=${build(tmp)}`);
rmSync(tmp, { recursive: true, force: true });
