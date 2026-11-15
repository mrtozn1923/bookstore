import { test as base, expect as baseExpect, type APIRequestContext } from '@playwright/test';
import { CartPage } from './pages/cart-page';
import { CatalogPage } from './pages/catalog-page';
import { apiURL } from './support/config';
import { priceMatchers } from './support/matchers';

export interface TestBook {
  id: number;
  isbn: string;
  title: string;
  price: number;
}

// Test verisi UI'dan değil, API'den ve her test için benzersiz adlarla hazırlanır
export class BookstoreData {
  private counter = 0;

  constructor(
    private readonly api: APIRequestContext,
    // Bu testin oluşturduğu bütün kitap adlarında geçer; aramada testin kendi verisini seçer
    readonly suffix: string,
  ) {}

  async book(title: string, price: number, stock = 10): Promise<TestBook> {
    const isbn = `978-${String(Math.floor(Math.random() * 1e10)).padStart(10, '0')}`;
    const response = await this.api.post('/api/books', {
      data: { isbn, title: `${title} ${this.unique()}`, price, stock },
    });
    expect(response.status(), await response.text()).toBe(201);
    return response.json();
  }

  async coupon(percentOff: number, validDays = 30): Promise<string> {
    const code = `SAVE${percentOff}-${this.unique()}`.toUpperCase();
    const validUntil = new Date(Date.now() + validDays * 24 * 60 * 60 * 1000).toISOString();
    const response = await this.api.post('/api/coupons', { data: { code, percentOff, validUntil } });
    expect(response.status(), await response.text()).toBe(201);
    return code;
  }

  private unique(): string {
    return `${this.suffix}${++this.counter}`;
  }
}

interface TestFixtures {
  data: BookstoreData;
  catalog: CatalogPage;
  cart: CartPage;
}

interface WorkerFixtures {
  adminApi: APIRequestContext;
}

export const test = base.extend<TestFixtures, WorkerFixtures>({
  adminApi: [
    async ({ playwright }, use) => {
      const anonymous = await playwright.request.newContext({ baseURL: apiURL });
      const tokenResponse = await anonymous.post('/e2e/token', { data: { customerId: 0, roles: ['admin'] } });
      const { accessToken } = await tokenResponse.json();
      await anonymous.dispose();

      const api = await playwright.request.newContext({
        baseURL: apiURL,
        extraHTTPHeaders: { Authorization: `Bearer ${accessToken}` },
      });
      await use(api);
      await api.dispose();
    },
    { scope: 'worker' },
  ],
  data: async ({ adminApi }, use, testInfo) => {
    await use(new BookstoreData(adminApi, `w${testInfo.workerIndex}t${Date.now().toString(36)}-`));
  },
  catalog: async ({ page }, use) => {
    await use(new CatalogPage(page));
  },
  cart: async ({ page }, use) => {
    await use(new CartPage(page));
  },
});

export const expect = baseExpect.extend(priceMatchers);
