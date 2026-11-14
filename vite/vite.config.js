import { NodePackageImporter } from "sass-embedded";
import { defineConfig } from "vite";

export default defineConfig({
    css: {
        // CSS source maps in the dev server (off by default)
        devSourcemap: true,
        preprocessorOptions: {
            scss: {
                // every .scss file starts with this line: it must not produce CSS
                additionalData: '@use "pkg:@bookstore/styles" as bs;\n',
                loadPaths: ["src/styles"],
                importers: [new NodePackageImporter()],
            },
        },
    },
});
