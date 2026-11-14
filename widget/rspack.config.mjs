import path from "node:path";
import { defineConfig } from "@rspack/cli";
import { rspack } from "@rspack/core";

// The same widget as webpack.config.mjs, with Rspack's built-in SWC loader, CSS and HTML plugin
export default defineConfig((env, argv) => {
    // Content hashes only in production: in dev the CSS hot update looks for the old file name
    const hash = argv.mode === "development" ? "" : ".[contenthash:8]";
    return {
    entry: { index: "./src/main.ts" },
    // Without targets SWC and the runtime fall back to old syntax (async/await becomes generator helpers)
    target: ["web", "es2022"],
    output: {
        path: path.resolve(import.meta.dirname, "dist-rspack"),
        filename: `[name]${hash}.js`,
        cssFilename: `[name]${hash}.css`,
        clean: true,
    },
    resolve: { extensions: [".ts", ".js"] },
    module: {
        rules: [
            { test: /\.ts$/, loader: "builtin:swc-loader", options: { jsc: { parser: { syntax: "typescript" } }, env: { targets: ["chrome 111", "edge 111", "firefox 114", "safari 16.4"] } }, type: "javascript/auto" },
            { test: /\.css$/, type: "css/auto" },
        ],
    },
    plugins: [
        // index.html loads /src/main.ts for Vite, so Rspack gets its own minimal page
        new rspack.HtmlRspackPlugin({
            title: "Bookstore book finder",
            templateContent: '<!doctype html><html lang="en"><head><meta charset="utf-8"><title>Bookstore book finder</title></head><body><div id="book-finder"></div></body></html>',
        }),
    ],
    };
});
