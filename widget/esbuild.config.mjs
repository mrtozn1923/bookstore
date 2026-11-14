import * as esbuild from "esbuild";
import { writeFile } from "node:fs/promises";
import { htmlPlugin } from "./esbuild/html-plugin.mjs";
import { noCommonJsLodash } from "./esbuild/no-commonjs-lodash.mjs";

const serve = process.argv.includes("--serve");

const options = {
    entryPoints: ["src/main.ts"],
    bundle: true,
    format: "esm",
    splitting: true,
    target: ["chrome111", "edge111", "firefox114", "safari16.4"],
    minify: !serve,
    sourcemap: serve,
    outdir: "dist-esbuild",
    entryNames: "[name]-[hash]",
    metafile: true,
    logLevel: "info",
    plugins: [noCommonJsLodash(), htmlPlugin({ template: "index.html", entry: "src/main.ts", liveReload: serve })],
};

if (serve) {
    const ctx = await esbuild.context(options);
    await ctx.watch();
    await ctx.serve({ servedir: "dist-esbuild", port: 8000 });
} else {
    const result = await esbuild.build(options).catch(() => process.exit(1));
    await writeFile("dist-esbuild/meta.json", JSON.stringify(result.metafile));
    console.log(await esbuild.analyzeMetafile(result.metafile));
}
