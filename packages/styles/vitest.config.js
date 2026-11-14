import { defineConfig } from "vitest/config";

export default defineConfig({
    test: {
        // re-run the tests when a .scss file changes in watch mode
        forceRerunTriggers: ["**/*.scss"],
    },
});
