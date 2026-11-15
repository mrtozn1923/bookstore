import { expect, test } from './fixtures';

test('the cart count and total follow the cart and the coupon', async ({ data, catalog, cart, page }) => {
  const dune = await data.book('Dune', 64.9);
  const emma = await data.book('Emma', 39.5);
  const coupon = await data.coupon(10);

  await catalog.open();
  await catalog.search(data.suffix);
  await catalog.addToCart(dune.title);
  await catalog.addToCart(dune.title);
  await catalog.addToCart(emma.title);
  await expect(page.getByRole('link', { name: 'Cart (3)' })).toBeVisible();

  await cart.open();
  await expect(page).toHaveURL('/cart');
  await expect(page).toHaveTitle('Cart | Bookstore');
  await expect(cart.lines).toHaveCount(2);
  await expect(cart.lines).toContainText([dune.title, emma.title]);
  await expect(cart.subtotal).toHaveText('Subtotal: ₺169.30');
  await expect(page.getByRole('table')).toMatchAriaSnapshot(`
    - table:
      - rowgroup:
        - row "Book Quantity Price"
      - rowgroup:
        - row /Dune .* 2 ₺129\\.80/
        - row /Emma .* 1 ₺39\\.50/
  `);

  await cart.applyCoupon(coupon);
  // Kupon HTTP isteğiyle doğrulanır; toplam cevap gelince değişir, assertion o anı bekler
  await expect(cart.total).toHaveText('Total: ₺152.37');
  await expect(cart.total).toShowPrice(152.37);
  await expect(page.getByLabel('Coupon code')).toHaveValue(coupon);
});

test('an order reduces the stock that the API reports', async ({ data, catalog, cart, adminApi, page }) => {
  const dune = await data.book('Dune', 64.9, 10);

  await catalog.open();
  await catalog.search(dune.title);
  await catalog.addToCart(dune.title);
  await catalog.addToCart(dune.title);
  await cart.open();
  await cart.placeOrder('Izmir');

  await expect(page).toHaveURL(/\/orders\/\d+$/);
  await expect(page).toHaveTitle('Order | Bookstore');
  await expect.soft(page.getByRole('heading', { level: 1 })).toHaveText('Order placed');
  await expect.soft(page.getByRole('definition').nth(1)).toShowPrice(129.8);
  await expect.soft(page.getByRole('definition').nth(2)).toHaveText('TRK-E2E-1');

  // Sayfada olmayan bir değeri, değişene kadar API'den tekrar tekrar oku
  await expect
    .poll(async () => (await (await adminApi.get(`/api/books/${dune.id}`)).json()).stock, {
      message: 'stock after the order',
    })
    .toBe(8);

  // Birden çok kontrol ya da action birlikte tekrar edilecekse toPass; varsayılan timeout'u 0, açıkça ver
  await expect(async () => {
    const response = await adminApi.get(`/api/books/${dune.id}`);
    expect(response.status()).toBe(200);
    expect(await response.json()).toMatchObject({ title: dune.title, stock: 8 });
  }).toPass({ intervals: [250, 500, 1_000], timeout: 10_000 });
});
