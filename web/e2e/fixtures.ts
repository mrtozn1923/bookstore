import { test as base, expect as baseExpect, type APIRequestContext, type PlaywrightWorkerArgs } from '@playwright/test';
import type { OrderDto } from '../src/app/api/book-api.service';
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
  customerApi: APIRequestContext;
  placedOrder: OrderDto;
  pageErrors: void;
}

interface WorkerFixtures {
  adminApi: APIRequestContext;
}

// E2E host'unun token endpoint'inden token alıp onu taşıyan bir API context'i açar
async function signedInApi(playwright: PlaywrightWorkerArgs['playwright'], body: object): Promise<APIRequestContext> {
  const anonymous = await playwright.request.newContext({ baseURL: apiURL });
  const { accessToken } = await (await anonymous.post('/e2e/token', { data: body })).json();
  await anonymous.dispose();
  return playwright.request.newContext({ baseURL: apiURL, extraHTTPHeaders: { Authorization: `Bearer ${accessToken}` } });
}

export const test = base.extend<TestFixtures, WorkerFixtures>({
  // Worker başına bir kez: yönetici token'ı yalnız veri hazırlamak için
  adminApi: [
    async ({ playwright }, use) => {
      const api = await signedInApi(playwright, { customerId: 0, roles: ['admin'] });
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
  // Ada adına API çağrıları; storageState'teki token'la aynı müşteri
  customerApi: async ({ playwright }, use) => {
    const api = await signedInApi(playwright, { customerId: 1 });
    await use(api);
    await api.dispose();
  },
  // Test başına yeni bir sipariş; test bitince iptal edilir
  placedOrder: async ({ data, customerApi }, use) => {
    const book = await data.book('Middlemarch', 48.75);
    const response = await customerApi.post('/api/orders', {
      data: { items: [{ bookId: book.id, quantity: 1 }], shippingCity: 'Izmir' },
    });
    expect(response.status(), await response.text()).toBe(201);
    const order: OrderDto = await response.json();

    await use(order);

    const cancelled = await customerApi.post(`/api/orders/${order.id}/cancel`);
    expect(cancelled.status(), 'teardown: cancel the seeded order').toBe(200);
  },
  // Her testte kendiliğinden çalışır: sayfada yakalanmamış bir JavaScript hatası testi kırar
  pageErrors: [
    async ({ page }, use) => {
      const errors: string[] = [];
      page.on('pageerror', (error) => errors.push(error.message));
      // Angular'ın global error listener'ları hatayı ErrorHandler'a verir: pageerror değil, "ERROR ..." console.error'ı gelir
      page.on('console', (message) => {
        if (message.type() === 'error' && message.text().startsWith('ERROR')) {
          errors.push(message.text().split('\n')[0]);
        }
      });
      await use();
      expect(errors, 'uncaught errors in the page').toEqual([]);
    },
    { auto: true },
  ],
});

export const expect = baseExpect.extend(priceMatchers);
