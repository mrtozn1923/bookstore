import type { Locator, Page } from '@playwright/test';

export class CartPage {
  readonly lines: Locator;
  readonly subtotal: Locator;
  readonly total: Locator;
  readonly error: Locator;

  constructor(private readonly page: Page) {
    this.lines = page.getByRole('table').getByRole('row').filter({ has: page.getByRole('cell') });
    this.subtotal = page.getByText(/^Subtotal:/);
    this.total = page.getByText(/^Total:/);
    this.error = page.getByRole('alert');
  }

  async open(): Promise<void> {
    // Sepet bellekte duruyor; page.goto('/cart') uygulamayı yeniden yükleyip sepeti boşaltır
    await this.page.getByRole('link', { name: /^Cart/ }).click();
  }

  async applyCoupon(code: string): Promise<void> {
    await this.page.getByLabel('Coupon code').fill(code);
    await this.page.getByRole('button', { name: 'Apply' }).click();
  }

  async placeOrder(city: string): Promise<void> {
    await this.page.getByLabel('Shipping city').fill(city);
    await this.page.getByRole('button', { name: 'Place order' }).click();
  }
}
