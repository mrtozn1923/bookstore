import { defineConfig, devices } from '@playwright/test';
import { apiPort, apiURL, customerStorageState, timezoneId, webPort, webURL } from './e2e/support/config';

const ci = !!process.env.CI;

export default defineConfig({
  testDir: './e2e',
  // Görsel testler platforma bağlı; playwright.visual.config.ts ile container'da koşar
  testIgnore: /visual\.spec\.ts/,
  fullyParallel: true,
  forbidOnly: ci,
  retries: ci ? 2 : 0,
  // Timeout katmanları: test 30 sn, assertion 5 sn; CI'da bütün koşu en fazla 20 dk
  timeout: 30_000,
  expect: { timeout: 5_000 },
  globalTimeout: ci ? 20 * 60_000 : undefined,
  reporter: ci
    ? [['blob'], ['junit', { outputFile: 'test-results/junit.xml' }], ['list']]
    : [['list'], ['html', { open: 'never' }]],
  use: {
    baseURL: webURL,
    locale: 'en-US',
    timezoneId,
    actionTimeout: 10_000,
    navigationTimeout: 15_000,
    trace: 'retain-on-failure',
    screenshot: 'only-on-failure',
    video: 'retain-on-failure',
  },
  projects: [
    { name: 'setup', testMatch: /.*\.setup\.ts/ },
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'], storageState: customerStorageState },
      dependencies: ['setup'],
    },
    {
      name: 'webkit',
      use: { ...devices['Desktop Safari'], storageState: customerStorageState },
      dependencies: ['setup'],
      grep: /@smoke/,
    },
    {
      name: 'mobile',
      use: { ...devices['Pixel 7'], storageState: customerStorageState },
      dependencies: ['setup'],
      grep: /@mobile/,
    },
  ],
  webServer: [
    {
      name: 'api',
      command: 'dotnet run --project ../tests/Bookstore.E2E.Host',
      url: `${apiURL}/api/books`,
      env: { BOOKSTORE_API_PORT: String(apiPort) },
      reuseExistingServer: !ci,
      timeout: 180_000,
      gracefulShutdown: { signal: 'SIGTERM', timeout: 10_000 },
    },
    {
      name: 'web',
      command: `npm start -- --port ${webPort}`,
      url: webURL,
      env: { BOOKSTORE_API_URL: apiURL },
      reuseExistingServer: !ci,
    },
  ],
});
