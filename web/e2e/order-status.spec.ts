import { expect, test } from './fixtures';

test('an order placed in the UI is checked and cancelled through the API', async ({
  data,
  catalog,
  cart,
  customerApi,
  page,
}) => {
  const book = await data.book('Emma', 39.5);

  await catalog.open();
  await catalog.search(book.title);
  await catalog.addToCart(book.title);
  await cart.open();
  await cart.placeOrder('Izmir');
  await expect(page).toHaveURL(/\/orders\/\d+$/);
  const id = Number(new URL(page.url()).pathname.split('/').pop());

  // Ekranın göstermediği alanlar API'den doğrulanır
  const response = await customerApi.get(`/api/orders/${id}`);
  await expect(response).toBeOK();
  expect(await response.json()).toMatchObject({ status: 'Placed', total: 39.5, trackingNumber: 'TRK-E2E-1' });

  // İptalin UI'ı yok; API'den yapılır, sonucu sayfada görülür
  await expect(await customerApi.post(`/api/orders/${id}/cancel`)).toBeOK();
  await page.reload();
  await expect(page.getByRole('heading', { level: 1 })).toHaveText('Order cancelled');
});
