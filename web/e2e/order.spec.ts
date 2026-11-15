import type { OrderDto } from '../src/app/api/book-api.service';
import { expect, test } from './fixtures';
import { timezoneId } from './support/config';

test('the order page shows the cancellation deadline', async ({ data, catalog, cart, page }) => {
  const book = await data.book('Middlemarch', 48.75);
  await catalog.open();
  await catalog.search(book.title);
  await catalog.addToCart(book.title);
  await cart.open();

  const created = page.waitForResponse((r) => r.url().endsWith('/api/orders') && r.request().method() === 'POST');
  await cart.placeOrder('Izmir');
  const order: OrderDto = await (await created).json();

  const deadline = new Date(Date.parse(order.createdAt) + 30 * 60 * 1000);
  // Beklenen saati tarayıcıyla aynı saat diliminde hesapla; test process'inin saat dilimine güvenme
  const expected = deadline.toLocaleTimeString('en-GB', { hour: '2-digit', minute: '2-digit', timeZone: timezoneId });
  await expect(page.getByText(`You can cancel this order until ${expected}.`)).toBeVisible();
});
