import { defineConfig } from '@playwright/test';

// Starts the API and the Angular dev server, runs the tests in Playwright's bundled headless Chromium.
export default defineConfig({
  testDir: './e2e',
  workers: 1,
  use: {
    baseURL: 'http://127.0.0.1:52806',
    browserName: 'chromium',
    headless: true,
    launchOptions: { args: ['--use-mock-keychain'] },
  },
  webServer: [
    {
      command: 'dotnet run --project ../src/Bookstore.Api --no-launch-profile --urls http://127.0.0.1:52801',
      url: 'http://127.0.0.1:52801/api/consent/config',
      env: { ASPNETCORE_ENVIRONMENT: 'Development' },
      reuseExistingServer: false,
      timeout: 180_000,
    },
    {
      command: 'npx ng serve --host 127.0.0.1 --port 52806 --proxy-config proxy.conf.json',
      url: 'http://127.0.0.1:52806',
      reuseExistingServer: false,
      timeout: 180_000,
    },
  ],
});
