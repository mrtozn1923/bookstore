import { expect, test } from './fixtures';

test('a customer finds a book, adds it to the cart and places an order', async ({ page, api }) => {
  await page.goto('/');

  await page.getByRole('searchbox', { name: 'Search books' }).fill('hobbit');
  const results = page.getByRole('list', { name: 'Books' }).getByRole('article');
  await expect(results).toHaveCount(1);
  await results.getByRole('button', { name: 'Add to cart' }).click();
  await expect(page.getByRole('link', { name: 'Cart (1)' })).toBeVisible();

  await page.getByRole('link', { name: 'Cart (1)' }).click();
  await page.getByRole('link', { name: 'Checkout' }).click();
  await page.getByLabel('Full name').fill('Ada Lovelace');
  await page.getByLabel('Email').fill('ada@example.com');
  await page.getByLabel('City').selectOption('Izmir');
  await page.getByRole('button', { name: 'Place order' }).click();

  await expect(page).toHaveURL('/orders/42');
  await expect(page.getByRole('heading', { name: 'Order BS-1042' })).toBeVisible();
  await expect(page.getByRole('list', { name: 'Order lines' })).toHaveText('1 × The Hobbit');
  expect(api.placedOrders).toEqual([
    { fullName: 'Ada Lovelace', email: 'ada@example.com', city: 'Izmir', lines: [{ isbn: '9780261103344', quantity: 1 }] },
  ]);
});

test('the cart survives a failed order', async ({ page, api }) => {
  api.failOrders = true;
  await page.goto('/');
  await page.getByRole('article').filter({ hasText: 'Dune' }).getByRole('button', { name: 'Add to cart' }).click();
  await page.getByRole('link', { name: 'Cart (1)' }).click();
  await page.getByRole('link', { name: 'Checkout' }).click();
  await page.getByLabel('Full name').fill('Ada Lovelace');
  await page.getByLabel('Email').fill('ada@example.com');
  await page.getByLabel('City').selectOption('Ankara');
  await page.getByRole('button', { name: 'Place order' }).click();

  await expect(page.getByRole('alert')).toHaveText('We could not place your order. Please try again.');
  await expect(page.getByRole('link', { name: 'Cart (1)' })).toBeVisible();
});
