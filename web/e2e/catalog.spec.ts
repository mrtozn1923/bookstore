import { expect, test } from './fixtures';

test('search finds a book with a lowercase term', async ({ data, catalog }) => {
  const book = await data.book('Persuasion', 41.5);

  await catalog.open();
  await catalog.search(book.title.toLowerCase());

  await expect(catalog.results).toHaveCount(1);
  await expect(catalog.results.getByRole('heading')).toHaveText(book.title);
});

test('adding a book updates the cart link', async ({ data, catalog, page }) => {
  const book = await data.book('Emma', 39.5);

  await catalog.open();
  await catalog.search(book.title);
  await catalog.addToCart(book.title);

  await expect(page.getByRole('link', { name: 'Cart (1)' })).toBeVisible();
  await expect(page.getByRole('status')).toHaveText(`${book.title} added to cart`);
});

test('an out-of-stock book cannot be added', async ({ data, catalog }) => {
  const book = await data.book('The Hobbit', 52, 0);

  await catalog.open();
  await catalog.search(book.title);

  await expect(catalog.card(book.title).getByText('Out of stock')).toBeVisible();
  await expect(catalog.card(book.title).getByRole('button', { name: 'Add to cart' })).toBeDisabled();
});
