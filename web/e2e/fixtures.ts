import { test as base, expect } from '@playwright/test';
import { MockApi } from './mock-api';

export const test = base.extend<{ api: MockApi }>({
  api: [
    async ({ page }, use) => {
      const api = new MockApi();
      await page.route('**/api/**', (route) => api.handle(route));
      await use(api);
      expect(api.unhandled, 'API requests without a mock').toEqual([]);
    },
    { auto: true },
  ],
});

export { expect };
