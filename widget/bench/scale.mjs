// Usage: npm run bench:scale -- 5000
// Generates N small ES modules (a tree of Bookstore "shelf" modules) and bundles + minifies them in
// the same process with four bundlers: 1 warm-up + 3 runs, median, ratio to the fastest.
import { mkdirSync, rmSync, writeFileSync } from "node:fs";
import path from "node:path";
import * as esbuild from "esbuild";
import { rspack } from "@rspack/core";
import { build as rolldown } from "rolldown";
import webpack from "webpack";

const count = Number(process.argv[2] ?? 1000);
const dir = path.resolve(import.meta.dirname, "generated");
const out = path.resolve(import.meta.dirname, "../dist-scale");
rmSync(dir, { recursive: true, force: true });
mkdirSync(dir, { recursive: true });

for (let i = 0; i < count; i++) {
    const parent = i === 0 ? "" : `import { shelf${Math.floor((i - 1) / 2)} } from "./shelf${Math.floor((i - 1) / 2)}.js";\n`;
    writeFileSync(path.join(dir, `shelf${i}.js`), `${parent}
const titles${i} = ["Dune", "Emma", "The Hobbit", "Middlemarch", "Persuasion"];
export function shelf${i}(prefix) {
    const books = titles${i}.map((title, index) => ({ title, price: (index + ${i}) * 10, shelf: ${i} }));
    const total = books.reduce((sum, book) => sum + book.price, 0);
    return \`\${prefix}/${i}: \${books.length} books, \${total} TRY\`${i === 0 ? "" : ` + " | " + shelf${Math.floor((i - 1) / 2)}(prefix)`};
}
`);
}
const imports = Array.from({ length: count }, (_, i) => `import { shelf${i} } from "./shelf${i}.js";`).join("\n");
const calls = Array.from({ length: count }, (_, i) => `shelf${i}("bookstore")`).join(",\n");
writeFileSync(path.join(dir, "entry.js"), `${imports}\nconsole.log([${calls}].length);\n`);
const entry = path.join(dir, "entry.js");

const compile = (create, config) => new Promise((resolve, reject) => create(config, (err, stats) =>
    (err || stats.hasErrors() ? reject(err ?? stats.toString()) : resolve())));

const tools = {
    esbuild: () => esbuild.build({ entryPoints: [entry], bundle: true, minify: true, format: "esm", outfile: `${out}/esbuild.js`, logLevel: "silent" }),
    rolldown: () => rolldown({ input: entry, logLevel: "silent", output: { file: `${out}/rolldown.js`, minify: true } }),
    rspack: () => compile(rspack, { mode: "production", entry, output: { path: out, filename: "rspack.js" }, infrastructureLogging: { level: "none" } }),
    webpack: () => compile(webpack, { mode: "production", entry, output: { path: out, filename: "webpack.js" }, infrastructureLogging: { level: "none" } }),
};

const results = [];
for (const [name, run] of Object.entries(tools)) {
    await run();
    const times = [];
    for (let i = 0; i < 3; i++) {
        const start = performance.now();
        await run();
        times.push(performance.now() - start);
    }
    times.sort((a, b) => a - b);
    results.push({ name, median: times[1] });
}
const fastest = Math.min(...results.map(result => result.median));
console.log(`${count} modules`);
for (const { name, median } of results) {
    console.log(`  ${name.padEnd(9)} ${(median / fastest).toFixed(1).padStart(5)}x`);
}
