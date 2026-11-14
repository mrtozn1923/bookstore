import { visualizer } from "rollup-plugin-visualizer";
import { defineConfig, normalizePath, type Plugin } from "vite";

// Runs with "npx vite build --mode graph": prints the module graph and what ended up in each chunk
function moduleGraphReport(): Plugin {
    return {
        name: "module-graph-report",
        apply: "build",
        generateBundle(_options, bundle) {
            const root = normalizePath(process.cwd()) + "/";
            const short = (id: string) => id.replace(root, "");
            const graph = [...this.getModuleIds()];
            const fromLodash = graph.filter(id => id.includes("/lodash-es/")).length;
            console.log(`\nmodule graph: ${graph.length} modules (${fromLodash} from lodash-es)`);
            for (const id of graph.filter(id => id.includes("/src/"))) {
                const imports = this.getModuleInfo(id)?.importedIds ?? [];
                console.log(`  ${short(id)} -> ${imports.map(short).join(", ") || "(none)"}`);
            }
            for (const output of Object.values(bundle)) {
                if (output.type !== "chunk") {
                    continue;
                }
                const rendered = Object.entries(output.modules).filter(([, m]) => m.renderedLength > 0);
                const lodash = rendered.filter(([id]) => id.includes("/lodash-es/")).length;
                console.log(`chunk ${output.fileName}: ${rendered.length} modules rendered (${lodash} from lodash-es)`);
            }
        },
    };
}

export default defineConfig(({ mode }) => ({
    plugins: [
        ...(mode === "graph" ? [moduleGraphReport()] : []),
        // npx vite build --mode analyze: stats/treemap.html (open in a browser) and stats/list.md
        ...(mode === "analyze"
            ? [
                visualizer({ filename: "stats/treemap.html", template: "treemap", gzipSize: true }),
                visualizer({ filename: "stats/list.md", template: "markdown", gzipSize: true }),
            ]
            : []),
    ],
    server: {
        port: 5173,
        strictPort: true,
        // Bookstore.Web (ASP.NET Core) runs at 5080: /api goes there, its pages load our modules
        proxy: { "/api/": "http://localhost:5080" },
        cors: { origin: "http://localhost:5080" },
    },
    // npx vite build --mode vendor: node_modules goes into its own long-lived chunk
    ...(mode === "vendor" && {
        build: {
            rolldownOptions: {
                output: {
                    codeSplitting: {
                        groups: [{ name: "vendor", test: /node_modules/ }],
                    },
                },
            },
        },
    }),
    ...(mode === "server" && {
        base: "/dist/",
        input: "src/main.ts",
        build: {
            manifest: true,
            outDir: "../server/Bookstore.Web/wwwroot/dist",
            emptyOutDir: true,
            modulePreload: { polyfill: false },
            // .map files without a sourceMappingURL comment; the JS and the map share a debugId
            sourcemap: "hidden",
            rolldownOptions: { output: { sourcemapDebugIds: true } },
        },
    }),
}));
