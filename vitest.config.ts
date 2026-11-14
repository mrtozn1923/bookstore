import { defineConfig } from "vitest/config";

// only the examples project's tests; bookstore-web and rxjs9-trial run their own
export default defineConfig({
  test: { include: ["src/**/*.test.ts"] },
});
