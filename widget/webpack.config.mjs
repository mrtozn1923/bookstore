import path from "node:path";
import webpack from "webpack";
import { AssetManifestPlugin } from "./webpack/asset-manifest-plugin.mjs";

export default webpack.defineConfig((env, argv) => ({
    entry: { index: "./index.html" },
    output: {
        path: path.resolve(import.meta.dirname, "dist-webpack"),
        filename: "[name].[contenthash:8].js",
        cssFilename: "[name].[contenthash:8].css",
        htmlFilename: "[name].html",
        clean: true,
    },
    module: {
        parser: {
            html: { chunkName: "book-finder" },
        },
        rules: [
            // Lets .ts imports omit the extension in a "type": "module" package
            { test: /\.ts$/, resolve: { fullySpecified: false } },
        ],
    },
    devtool: argv.mode === "production" ? false : "eval-source-map",
    plugins: [new AssetManifestPlugin()],
}));
