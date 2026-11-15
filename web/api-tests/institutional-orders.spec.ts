import { expect, test } from './api-fixtures';

test('a buyer places a bulk order and gets its invoice', async ({ data, buyerApi }) => {
  const emma = await data.book('Emma', 39.5, 50);

  const response = await buyerApi.post('/api/institutional/orders', {
    data: { lines: [{ isbn: emma.isbn, quantity: 20 }] },
  });
  await expect(response).toBeOK();
  expect(response.status()).toBe(201);
  const order = await response.json();
  expect(order).toMatchObject({ status: 'Placed', total: 711 });
  expect(response.headers()['location']).toBe(`/api/orders/${order.id}`);

  const invoice = await buyerApi.get(`/api/orders/${order.id}/invoice`);
  await expect(invoice).toBeOK();
  expect(invoice.headers()['content-type']).toBe('application/pdf');
  expect(invoice.headers()['content-disposition']).toContain(`filename=invoice-${order.reference}.pdf`);
  expect((await invoice.body()).subarray(0, 5).toString()).toBe('%PDF-');
});

test('fewer than 20 copies is a validation problem', async ({ data, buyerApi }) => {
  const emma = await data.book('Emma', 39.5, 50);

  const response = await buyerApi.post('/api/institutional/orders', {
    data: { lines: [{ isbn: emma.isbn, quantity: 19 }] },
  });

  expect(response.status()).toBe(400);
  expect(response.headers()['content-type']).toBe('application/problem+json');
  expect((await response.json()).errors).toEqual({ lines: ['Institutional orders need at least 20 copies.'] });
});

test('only institution buyers may place bulk orders', async ({ data, customerApi, request }) => {
  const emma = await data.book('Emma', 39.5, 50);
  const body = { data: { lines: [{ isbn: emma.isbn, quantity: 20 }] } };

  // request: config'deki baseURL'i kullanan, token'sız yerleşik fixture
  expect((await request.post('/api/institutional/orders', body)).status()).toBe(401);
  expect((await customerApi.post('/api/institutional/orders', body)).status()).toBe(403);
});

test('an invoice is visible only to the buyer who ordered', async ({ data, buyerApi, customerApi }) => {
  const emma = await data.book('Emma', 39.5, 50);
  const order = await (
    await buyerApi.post('/api/institutional/orders', { data: { lines: [{ isbn: emma.isbn, quantity: 20 }] } })
  ).json();

  expect((await customerApi.get(`/api/orders/${order.id}/invoice`)).status()).toBe(404);
});
