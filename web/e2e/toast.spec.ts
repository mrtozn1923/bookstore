import { expect, test } from './fixtures';

test('the cart toast disappears after three seconds', async ({ data, catalog, page }) => {
  const book = await data.book('Emma', 39.5);
  await page.clock.install();

  await catalog.open();
  await catalog.search(book.title);
  await catalog.addToCart(book.title);
  await expect(page.getByRole('status')).toBeVisible();

  await page.clock.runFor(2_900);
  await expect(page.getByRole('status')).toBeVisible();
  await page.clock.runFor(200);
  await expect(page.getByRole('status')).toBeHidden();
});
