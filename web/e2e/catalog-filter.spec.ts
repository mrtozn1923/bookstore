import { expect, test } from './fixtures';

test('finds the Dune edition that is in stock', async ({ data, catalog, page }) => {
  await data.book('Dune Hardcover', 89.9, 0);
  const paperback = await data.book('Dune Paperback', 64.9, 5);

  await catalog.open();
  await catalog.search(data.suffix);

  const dune = catalog.results.filter({ hasText: 'Dune' });
  await expect(dune).toHaveCount(2);

  // filter, kartın kendisini değil içeriğini sorgular: metin kartın herhangi bir yerinde geçebilir
  const inStock = dune.filter({ hasNotText: 'Out of stock' });
  await expect(inStock.getByRole('heading')).toHaveText(paperback.title);
  await expect(dune.getByRole('button', { name: 'Add to cart', disabled: true })).toHaveCount(1);

  await inStock.getByRole('button', { name: 'Add to cart' }).click();
  await expect(page.getByRole('link', { name: 'Cart (1)' })).toBeVisible();
  await expect(page.getByRole('status')).toHaveText(`${paperback.title} added to cart`);
});

test('the search box is a searchbox with a visible label', async ({ page }) => {
  await page.goto('/');
  // and: iki locator'ın aynı element'i gösterdiğini iddia eder
  await expect(page.getByRole('searchbox').and(page.getByLabel('Search books'))).toBeVisible();
});
