// Bundles web/native-search.ts into one file for the browser check
import { build } from "vite";

await build({
  logLevel: "silent",
  build: {
    outDir: "dist/web",
    emptyOutDir: true,
    rollupOptions: { input: "web/native-search.ts", output: { entryFileNames: "native-search.js" } },
  },
});
