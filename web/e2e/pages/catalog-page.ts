import type { Locator, Page } from '@playwright/test';

export class CatalogPage {
  readonly results: Locator;

  constructor(private readonly page: Page) {
    this.results = page.getByRole('list', { name: 'Search results' }).getByRole('listitem');
  }

  async open(): Promise<void> {
    await this.page.goto('/');
  }

  async search(term: string): Promise<void> {
    await this.page.getByLabel('Search books').fill(term);
    await this.page.getByRole('button', { name: 'Search' }).click();
  }

  card(title: string): Locator {
    return this.page.getByRole('listitem').filter({
      has: this.page.getByRole('heading', { name: title, exact: true }),
    });
  }

  async addToCart(title: string): Promise<void> {
    await this.card(title).getByRole('button', { name: 'Add to cart' }).click();
  }
}
