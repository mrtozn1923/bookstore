import { stat } from "node:fs/promises";
import path from "node:path";
import * as esbuild from "esbuild";
import { build as rolldown } from "rolldown";
import webpack from "webpack";

// Bundles and minifies all of lodash-es (640 modules) with three bundlers, in the same process
const entry = path.resolve(import.meta.dirname, "lodash-all.js");
const out = path.resolve(import.meta.dirname, "../dist-bench");

const tools = {
    esbuild: () => esbuild.build({ entryPoints: [entry], bundle: true, minify: true, format: "esm", outfile: `${out}/esbuild.js`, logLevel: "silent" }),
    rolldown: () => rolldown({ input: entry, logLevel: "silent", output: { file: `${out}/rolldown.js`, minify: true } }),
    webpack: () => new Promise((resolve, reject) => webpack({
        mode: "production", entry, output: { path: out, filename: "webpack.js" }, infrastructureLogging: { level: "none" },
    }, (err, stats) => (err || stats.hasErrors() ? reject(err ?? stats.toString()) : resolve()))),
};

for (const [name, run] of Object.entries(tools)) {
    await run();
    const times = [];
    for (let i = 0; i < 5; i++) {
        const start = performance.now();
        await run();
        times.push(performance.now() - start);
    }
    times.sort((a, b) => a - b);
    const { size } = await stat(`${out}/${name}.js`);
    console.log(`${name.padEnd(9)} median ${Math.round(times[2])} ms, output ${(size / 1024).toFixed(1)} KiB`);
}
