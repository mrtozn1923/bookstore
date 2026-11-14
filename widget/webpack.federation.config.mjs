import path from "node:path";
import webpack from "webpack";

const { ModuleFederationPlugin } = webpack.container;

export default webpack.defineConfig({
    entry: {},
    output: {
        path: path.resolve(import.meta.dirname, "dist-federation"),
        publicPath: "auto",
        uniqueName: "bookFinder",
        clean: true,
        copy: "./federation",
    },
    module: {
        rules: [{ test: /\.ts$/, resolve: { fullySpecified: false } }],
    },
    plugins: [
        new ModuleFederationPlugin({
            name: "bookFinder",
            filename: "remoteEntry.js",
            exposes: { "./finder": "./src/finder.ts" },
            shared: { "lodash-es": { singleton: true } },
        }),
    ],
});
