import { test as base, expect, type APIRequestContext } from '@playwright/test';
import { BookstoreData } from '../e2e/fixtures';
import { signedInApi } from '../e2e/support/api';

interface ApiFixtures {
  data: BookstoreData;
  buyerApi: APIRequestContext;
  customerApi: APIRequestContext;
}

// API testleri tarayıcı açmaz: page'e bağlı fixture'lar (pageErrors gibi) burada yok
export const test = base.extend<ApiFixtures, { adminApi: APIRequestContext }>({
  adminApi: [
    async ({ playwright }, use) => {
      const api = await signedInApi(playwright, { customerId: 0, roles: ['admin'] });
      await use(api);
      await api.dispose();
    },
    { scope: 'worker' },
  ],
  data: async ({ adminApi }, use, testInfo) => {
    await use(new BookstoreData(adminApi, `api${testInfo.workerIndex}t${Date.now().toString(36)}-`));
  },
  buyerApi: async ({ playwright }, use) => {
    const api = await signedInApi(playwright, { customerId: 2, roles: ['institution-buyer'] });
    await use(api);
    await api.dispose();
  },
  customerApi: async ({ playwright }, use) => {
    const api = await signedInApi(playwright, { customerId: 1 });
    await use(api);
    await api.dispose();
  },
});

export { expect };
