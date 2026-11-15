import { defineConfig, devices } from '@playwright/test';
import { apiURL, customerStorageState, timezoneId } from './e2e/support/config';

export default defineConfig({
  testDir: './e2e',
  // Görsel testler platforma bağlı; playwright.visual.config.ts ile container'da koşar
  testIgnore: /visual\.spec\.ts/,
  fullyParallel: true,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 0,
  reporter: [['list'], ['html', { open: 'never' }]],
  use: {
    baseURL: 'http://localhost:4200',
    timezoneId,
    trace: 'retain-on-failure',
    screenshot: 'only-on-failure',
  },
  projects: [
    { name: 'setup', testMatch: /.*\.setup\.ts/ },
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'], storageState: customerStorageState },
      dependencies: ['setup'],
    },
  ],
  webServer: [
    {
      name: 'api',
      command: 'dotnet run --project ../tests/Bookstore.E2E.Host',
      url: `${apiURL}/api/books`,
      reuseExistingServer: !process.env.CI,
      timeout: 180_000,
      gracefulShutdown: { signal: 'SIGTERM', timeout: 10_000 },
    },
    {
      name: 'web',
      command: 'npm start',
      url: 'http://localhost:4200',
      reuseExistingServer: !process.env.CI,
    },
  ],
});
