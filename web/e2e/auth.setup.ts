import { test as setup, type APIRequestContext, type Page } from '@playwright/test';
import { apiURL, buyerStorageState, customerStorageState } from './support/config';

// Production'da token identity provider'dan gelir; E2E host'u onun yerini tutuyor
async function signIn(page: Page, request: APIRequestContext, body: object, path: string): Promise<void> {
  const response = await request.post(`${apiURL}/e2e/token`, { data: body });
  const { accessToken } = await response.json();

  await page.goto('/');
  await page.evaluate((token) => localStorage.setItem('bookstore.accessToken', token), accessToken);
  await page.context().storageState({ path });
}

setup('sign in as Ada', async ({ page, request }) => {
  await signIn(page, request, { customerId: 1 }, customerStorageState);
});

setup('sign in as Nevin (institution buyer)', async ({ page, request }) => {
  await signIn(page, request, { customerId: 2, roles: ['institution-buyer'] }, buyerStorageState);
});
