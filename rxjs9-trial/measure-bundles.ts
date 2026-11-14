// Builds the same search pipeline with RxJS 6, 7 and 9 and prints the minified and gzipped size of each bundle.
import { readFileSync } from "node:fs";
import { gzipSync } from "node:zlib";
import { build } from "vite";

for (const version of ["v6", "v7", "v9"]) {
  await build({
    logLevel: "silent",
    // like the Angular CLI: prefer the ES2015 builds of RxJS 6 and 7 over their ES5 builds
    resolve: { conditions: ["es2015", "module", "browser", "production"], mainFields: ["es2015", "module"] },
    build: {
      outDir: `dist/${version}`,
      emptyOutDir: true,
      minify: true,
      rollupOptions: { input: `bundles/${version}.ts`, output: { entryFileNames: "search.js" } },
    },
  });
  const code = readFileSync(`dist/${version}/search.js`);
  const kb = (bytes: number) => `${(bytes / 1024).toFixed(1)} kB`;
  console.log(`${version}: ${kb(code.length).padStart(8)} minified, ${kb(gzipSync(code).length).padStart(7)} gzip`);
}
