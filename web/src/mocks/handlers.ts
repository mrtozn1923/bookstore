import { http, HttpResponse } from 'msw';
import { Order } from '../app/orders/order';
import { catalogOf } from '../testing/books';

export const placedOrder: Order = {
  id: 42,
  reference: 'BS-1042',
  status: 'Placed',
  total: 1280,
  lines: [
    { isbn: '9780441013593', title: 'Dune', quantity: 2, price: 450 },
    { isbn: '9780261103344', title: 'The Hobbit', quantity: 1, price: 380 },
  ],
  createdAt: '2026-11-14T09:30:00Z',
};

export const handlers = [
  http.get('/api/books', () => HttpResponse.json(catalogOf(30))),
  http.get('/api/books/:isbn/rating', () => HttpResponse.json({ average: 4.5, count: 12 })),
  http.get('/api/orders/:id', ({ params }) =>
    params['id'] === String(placedOrder.id)
      ? HttpResponse.json(placedOrder)
      : HttpResponse.json({ title: 'Not Found', status: 404 }, { status: 404 }),
  ),
  http.post('/api/orders', () => HttpResponse.json(placedOrder, { status: 201 })),
  http.post<never, { email: string; password: string }>('/api/auth/sign-in', async ({ request }) => {
    const { email, password } = await request.json();
    return password === 'correct-horse'
      ? HttpResponse.json({ email, token: 'test-token' })
      : HttpResponse.json({ title: 'Unauthorized', status: 401 }, { status: 401 });
  }),
];
