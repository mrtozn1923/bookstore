import { defineConfig, devices } from '@playwright/test';

// E2E testleri model ya da API olmadan çalışır: /chat/* istekleri her testte page.route ile cevaplanır
export default defineConfig({
  testDir: './e2e',
  fullyParallel: true,
  reporter: 'list',
  use: {
    baseURL: 'http://localhost:4260',
    trace: 'retain-on-failure',
  },
  projects: [{ name: 'chromium', use: { ...devices['Desktop Chrome'] } }],
  webServer: {
    command: 'npm start',
    url: 'http://localhost:4260',
    reuseExistingServer: !process.env['CI'],
  },
});
