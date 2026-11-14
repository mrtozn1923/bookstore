import path from "node:path";
import HtmlWebpackPlugin from "html-webpack-plugin";
import MiniCssExtractPlugin from "mini-css-extract-plugin";

// The pre-5.109 way: CSS through a loader chain, HTML through a plugin
export default {
    entry: { "book-finder": "./src/main.ts" },
    output: {
        path: path.resolve(import.meta.dirname, "dist-loaders"),
        filename: "[name].[contenthash:8].js",
        clean: true,
    },
    module: {
        rules: [
            { test: /\.ts$/, resolve: { fullySpecified: false } },
            { test: /\.css$/, use: [MiniCssExtractPlugin.loader, "css-loader"] },
        ],
    },
    plugins: [
        new MiniCssExtractPlugin({ filename: "[name].[contenthash:8].css" }),
        new HtmlWebpackPlugin({ title: "Bookstore book finder", templateContent: '<div id="book-finder"></div>' }),
    ],
};
