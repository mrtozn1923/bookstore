// Builds the Bookstore components step by step with both approaches and measures the CSS.
import { execFileSync } from "node:child_process";
import { mkdirSync, readFileSync, writeFileSync, rmSync } from "node:fs";
import { gzipSync } from "node:zlib";

const steps = ["book-card", "cart-line", "order-summary"];
const cli = "node_modules/.bin/tailwindcss";
const build = (input, cwd) =>
    execFileSync(cli, ["-i", input, "--minify"], { cwd, env: { ...process.env, NO_COLOR: "1" }, stdio: ["ignore", "pipe", "ignore"] }).toString();
const size = (css) => `${String(css.length).padStart(5)} B, gzip ${String(gzipSync(css).length).padStart(4)} B`;

rmSync("tmp", { recursive: true, force: true });
for (let k = 1; k <= steps.length; k++) {
    const used = steps.slice(0, k);
    const dir = `tmp/step-${k}`;
    mkdirSync(dir, { recursive: true });

    // semantic: hand-written CSS, only minified
    writeFileSync(`${dir}/semantic.css`, used.map((s) => readFileSync(`01/semantic/${s}.css`, "utf8")).join("\n"));
    const semantic = build(`${dir}/semantic.css`, ".");

    // utility-first: markup only, Tailwind generates the CSS (no Preflight here)
    writeFileSync(`${dir}/index.html`, used.map((s) => readFileSync(`01/utility/${s}.html`, "utf8")).join("\n"));
    writeFileSync(`${dir}/input.css`, [
        "@layer theme, base, components, utilities;",
        '@import "tailwindcss/theme.css" layer(theme);',
        '@import "tailwindcss/utilities.css" layer(utilities) source(none);',
        '@source "./index.html";',
    ].join("\n"));
    const utility = build(`${dir}/input.css`, ".");
    const classes = new Set(readFileSync(`${dir}/index.html`, "utf8").match(/class="([^"]+)"/g).flatMap((m) => m.slice(7, -1).split(" ")));

    console.log(`step ${k} (+${steps[k - 1]})`);
    console.log(`  semantic CSS : ${size(semantic)}`);
    console.log(`  utility CSS  : ${size(utility)}  (${classes.size} distinct classes)`);
}
rmSync("tmp", { recursive: true, force: true });
