import { expect, test } from './fixtures';

// Playwright Test Agents'ın başlangıç noktası: Ada olarak giriş yapılmış, katalog açık
test.describe('Bookstore', () => {
  test('seed', async ({ catalog, page }) => {
    await catalog.open();
    await expect(page.getByRole('heading', { name: 'Catalog' })).toBeVisible();
  });
});
