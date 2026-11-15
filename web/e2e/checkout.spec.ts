import { expect, test } from './fixtures';

test('a customer orders a book with a coupon', async ({ data, catalog, cart, page }) => {
  const book = await data.book('Dune', 64.9);
  const coupon = await data.coupon(10);

  await catalog.open();
  await catalog.search(book.title);
  await catalog.addToCart(book.title);

  await cart.open();
  await cart.applyCoupon(coupon);
  await expect(cart.total).toHaveText('Total: ₺58.41');
  await cart.placeOrder('Izmir');

  await expect(page.getByRole('heading', { name: 'Order placed' })).toBeVisible();
  await expect(page.getByText('₺58.41')).toBeVisible();
  await expect(page.getByText('TRK-E2E-1')).toBeVisible();
});

test('a payment outage is shown to the customer', async ({ data, catalog, cart, page }) => {
  const book = await data.book('Emma', 39.5);
  // Sahte ödeme sağlayıcısı E2E'de hep onay veriyor; hata durumunu tarayıcı tarafında üretiyoruz
  await page.route('**/api/orders', (route) =>
    route.fulfill({
      status: 503,
      contentType: 'application/problem+json',
      json: { status: 503, title: 'Payment provider unavailable' },
    }),
  );

  await catalog.open();
  await catalog.search(book.title);
  await catalog.addToCart(book.title);
  await cart.open();
  await cart.placeOrder('Izmir');

  await expect(cart.error).toHaveText('Payment provider unavailable');
  await expect(page.getByRole('button', { name: 'Place order' })).toBeEnabled();
});
