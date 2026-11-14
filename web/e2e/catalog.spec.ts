import { expect, test } from './fixtures';

test.use({ storageState: { cookies: [], origins: [] } });

test('sorts the catalog by price with the Material select', async ({ page }) => {
  await page.goto('/');

  await page.getByRole('combobox', { name: 'Sort by' }).click();
  await page.getByRole('option', { name: 'Price: low to high' }).click();

  await expect(page.getByRole('article').getByRole('heading')).toHaveText(['The Hobbit', 'Dune']);
});

test('a signed-out visitor is sent to sign in before checkout', async ({ page }) => {
  await page.goto('/');
  await page.getByRole('article').filter({ hasText: 'Dune' }).getByRole('button', { name: 'Add to cart' }).click();
  await page.getByRole('link', { name: 'Cart (1)' }).click();
  await page.getByRole('link', { name: 'Checkout' }).click();

  await expect(page).toHaveURL('/sign-in?returnUrl=%2Fcheckout');
});
