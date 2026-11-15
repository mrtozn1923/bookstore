import { defineConfig, devices } from '@playwright/test';
import { timezoneId } from './e2e/support/config';

// Görsel testler yalnız Playwright'ın Linux image'ında koşar; baseline'lar o ortamda üretilir
export default defineConfig({
  testDir: './e2e',
  testMatch: /visual\.spec\.ts/,
  reporter: [['list'], ['html', { open: 'never' }]],
  use: { baseURL: 'http://localhost:4300', timezoneId },
  projects: [{ name: 'chromium', use: { ...devices['Desktop Chrome'] } }],
  webServer: {
    command: 'node e2e/support/static-server.mjs dist/bookstore-web/browser 4300',
    url: 'http://localhost:4300',
  },
});
