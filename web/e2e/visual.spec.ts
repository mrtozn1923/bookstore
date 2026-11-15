import { expect, test } from '@playwright/test';

// Görsel testin verisi sabit olmalı: kitap listesi API yerine bu diziden gelir
const books = [
  { id: 1, isbn: '978-0441172719', title: 'Dune', price: 64.9, stock: 7 },
  { id: 2, isbn: '978-0141439587', title: 'Emma', price: 39.5, stock: 3 },
  { id: 4, isbn: '978-0141439549', title: 'Middlemarch', price: 48.75, stock: 5 },
  { id: 3, isbn: '978-0547928227', title: 'The Hobbit', price: 52, stock: 0 },
];

test.beforeEach(async ({ page }) => {
  await page.route((url) => url.pathname === '/api/books', (route) => route.fulfill({ json: books }));
  await page.goto('/');
  await expect(page.getByRole('list', { name: 'Search results' }).getByRole('listitem')).toHaveCount(4);
});

test('catalog page', async ({ page }) => {
  await expect(page).toHaveScreenshot('catalog.png');
});

test('toast after adding to cart', async ({ page }) => {
  await page.getByRole('listitem').filter({ hasText: 'Emma' }).getByRole('button', { name: 'Add to cart' }).click();
  await expect(page).toHaveScreenshot('catalog-toast.png');
});
