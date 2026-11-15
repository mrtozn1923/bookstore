import { defineConfig } from '@playwright/test';

// The a11y suite: axe on Bookstore's pages and states, against the mock API and the built app.
export default defineConfig({
  testDir: 'tests',
  // One worker: the tests share the baseline bookkeeping in afterAll
  workers: 1,
  reporter: process.env.CI ? [['list'], ['json', { outputFile: 'reports/axe-results.json' }]] : 'list',
  use: { baseURL: process.env.BOOKSTORE_URL ?? 'http://127.0.0.1:52411' },
  webServer: {
    command: 'node mock-api/server.mjs',
    cwd: '..',
    url: 'http://127.0.0.1:52411/api/categories',
    reuseExistingServer: !process.env.CI,
  },
});
