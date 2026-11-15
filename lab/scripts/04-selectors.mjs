// Prints the utilities Tailwind generates for single classes (each built on its own).
import { execFileSync } from "node:child_process";
import { mkdtempSync, rmSync, writeFileSync } from "node:fs";
import { join } from "node:path";

const dir = mkdtempSync("tw-sel-");
for (const cls of process.argv.slice(2)) {
    writeFileSync(join(dir, "in.css"), `@import "tailwindcss" source(none);\n@source inline("${cls}");\n`);
    const css = execFileSync("node_modules/.bin/tailwindcss", ["-i", join(dir, "in.css")], { stdio: ["ignore", "pipe", "ignore"] }).toString();
    const start = css.indexOf("@layer utilities {");
    const end = css.indexOf("\n}\n", start);
    const body = css.slice(start + "@layer utilities {".length, end).replace(/^\n/, "");
    console.log(`${cls}\n${body.replace(/^  /gm, "  ")}\n`);
}
rmSync(dir, { recursive: true, force: true });
