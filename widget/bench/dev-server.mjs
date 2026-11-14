// Usage: npm run bench:dev
// For each dev server: time from start until the widget is on screen in Chromium, then the time
// until a CSS change and a TypeScript change are visible in the open page (median of 5 edits).
import { spawn } from "node:child_process";
import { readFileSync, writeFileSync } from "node:fs";
import { createServer } from "node:net";
import { chromium } from "playwright";

const servers = {
    vite: (port) => ["node_modules/vite/bin/vite.js", "--port", String(port)],
    rspack: (port) => ["node_modules/@rspack/cli/bin/rspack.js", "serve", "--mode", "development", "--port", String(port)],
    webpack: (port) => ["node_modules/webpack-cli/bin/cli.js", "serve", "--mode", "development", "--port", String(port)],
};

const freePort = () => new Promise(resolve => {
    const server = createServer().listen(0, () => {
        const { port } = server.address();
        server.close(() => resolve(port));
    });
});

async function timeUntil(page, edit, check, expected) {
    const start = performance.now();
    edit();
    for (;;) {
        try {
            // A full page reload destroys the context while waiting; then we simply wait again
            await page.waitForFunction(check, expected, { timeout: 30_000, polling: 10 });
            return performance.now() - start;
        } catch (error) {
            if (performance.now() - start > 30_000) {
                throw error;
            }
        }
    }
}

const median = (values) => values.sort((a, b) => a - b)[Math.floor(values.length / 2)];
const browser = await chromium.launch();
const results = [];
const css = readFileSync("src/styles.css", "utf8");
const finder = readFileSync("src/finder.ts", "utf8");

for (const [name, args] of Object.entries(servers)) {
    const port = await freePort();
    const start = performance.now();
    const child = spawn(process.execPath, args(port), { stdio: "ignore" });
    let exitCode = null;
    child.on("exit", code => { exitCode = code; });
    const page = await browser.newPage();
    for (;;) {
        if (exitCode !== null) {
            throw new Error(`${name} dev server exited with code ${exitCode}`);
        }
        try {
            await page.goto(`http://localhost:${port}/`, { timeout: 2_000 });
            await page.waitForSelector(".finder-results li", { timeout: 10_000 });
            break;
        } catch {
            await new Promise(resolve => setTimeout(resolve, 50));
        }
    }
    const firstRender = performance.now() - start;

    const cssTimes = [];
    const tsTimes = [];
    try {
        for (let round = 1; round <= 5; round++) {
            const color = `rgb(${round * 40}, 0, 0)`;
            cssTimes.push(await timeUntil(page,
                () => writeFileSync("src/styles.css", css.replace("border-bottom: 1px solid #e2e8f0;", `border-bottom: 1px solid ${color};`)),
                (expected) => getComputedStyle(document.querySelector(".finder-results li")).borderBottomColor === expected, color));
            const placeholder = `Search books (${round})`;
            tsTimes.push(await timeUntil(page,
                () => writeFileSync("src/finder.ts", finder.replace("Search by title or author", placeholder)),
                (expected) => document.querySelector(".finder-input")?.placeholder === expected, placeholder));
        }
    } finally {
        writeFileSync("src/styles.css", css);
        writeFileSync("src/finder.ts", finder);
        await page.close();
        child.kill();
    }
    results.push({ name, firstRender, css: median(cssTimes), ts: median(tsTimes) });
}
await browser.close();

// Ratios to the fastest tool for each measurement
const ratio = (key, value) => `${(value / Math.min(...results.map(result => result[key]))).toFixed(1)}x`;
console.log("tool      first render  CSS change  TS change");
for (const result of results) {
    console.log(`${result.name.padEnd(9)} ${ratio("firstRender", result.firstRender).padStart(12)} ${ratio("css", result.css).padStart(11)} ${ratio("ts", result.ts).padStart(10)}`);
}
