// Usage: npm run bench:build
// Runs each tool's production build through its own CLI (1 warm-up + 5 runs) and prints the median,
// the ratio to the fastest tool and the gzip size of the emitted JS and CSS.
import { spawnSync } from "node:child_process";
import { readdirSync, readFileSync } from "node:fs";
import path from "node:path";
import { gzipSync } from "node:zlib";

const node = process.execPath;
const tools = {
    "vite (Rolldown)": { args: ["node_modules/vite/bin/vite.js", "build"], out: "dist" },
    "esbuild": { args: ["esbuild.config.mjs"], out: "dist-esbuild" },
    "rspack": { args: ["node_modules/@rspack/cli/bin/rspack.js", "build"], out: "dist-rspack" },
    "webpack": { args: ["node_modules/webpack-cli/bin/cli.js", "--mode", "production"], out: "dist-webpack" },
};

function gzipKiB(dir) {
    const files = readdirSync(dir, { recursive: true }).filter(file => /\.(js|css)$/.test(file));
    const bytes = files.reduce((sum, file) => sum + gzipSync(readFileSync(path.join(dir, file))).length, 0);
    return (bytes / 1024).toFixed(2);
}

const results = [];
for (const [name, { args, out }] of Object.entries(tools)) {
    const times = [];
    for (let run = 0; run < 6; run++) {
        const start = performance.now();
        const result = spawnSync(node, args, { stdio: "ignore" });
        if (result.status !== 0) {
            throw new Error(`${name} failed`);
        }
        if (run > 0) {
            times.push(performance.now() - start);
        }
    }
    times.sort((a, b) => a - b);
    results.push({ name, median: times[2], size: gzipKiB(out) });
}

const fastest = Math.min(...results.map(result => result.median));
for (const { name, median, size } of results) {
    console.log(`${name.padEnd(16)} ${(median / fastest).toFixed(1).padStart(5)}x  ${size} KiB gzip (JS + CSS)`);
}
