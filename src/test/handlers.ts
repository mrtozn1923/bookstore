import { http, HttpResponse } from 'msw';
import type { Cart } from '@/features/cart/types';
import type { BookSummary, PagedResult } from '@/features/catalog/types';
import { testBooks } from '@/test/books';

// In-memory carts, like the real API keeps them in its database; cleared after every test
export const carts = new Map<string, Cart>();

function cartView(cart: Cart): Cart {
  return { ...cart, total: cart.items.reduce((sum, line) => sum + line.lineTotal, 0) };
}

// The same URLs and response shapes as Bookstore.Api; fetch in the code under test is not changed at all
export const handlers = [
  http.get('/api/books', ({ request }) => {
    const url = new URL(request.url);
    const search = (url.searchParams.get('search') ?? '').toLowerCase();
    const page = Number(url.searchParams.get('page') ?? '1');
    const pageSize = Number(url.searchParams.get('pageSize') ?? '8');
    const found = testBooks.filter((b) => b.title.toLowerCase().includes(search) || b.author.toLowerCase().includes(search));
    const items: BookSummary[] = found.slice((page - 1) * pageSize, page * pageSize)
      .map(({ isbn, title, author, category, price, publishedYear }) => ({ isbn, title, author, category, price, publishedYear }));
    return HttpResponse.json<PagedResult<BookSummary>>({ items, total: found.length, page, pageSize });
  }),

  http.get('/api/books/:isbn', ({ params }) => {
    const book = testBooks.find((b) => b.isbn === params.isbn);
    return book ? HttpResponse.json(book) : new HttpResponse(null, { status: 404 });
  }),

  http.post('/api/carts', () => {
    const cart: Cart = { id: crypto.randomUUID(), items: [], total: 0, currency: 'TRY' };
    carts.set(cart.id, cart);
    return HttpResponse.json(cart, { status: 201 });
  }),

  http.get('/api/carts/:cartId', ({ params }) => {
    const cart = carts.get(String(params.cartId));
    return cart ? HttpResponse.json(cartView(cart)) : new HttpResponse(null, { status: 404 });
  }),

  http.put<{ cartId: string; isbn: string }, { quantity: number }>('/api/carts/:cartId/items/:isbn', async ({ params, request }) => {
    const cart = carts.get(params.cartId);
    const book = testBooks.find((b) => b.isbn === params.isbn);
    if (!cart || !book) {
      return new HttpResponse(null, { status: 404 });
    }
    const { quantity } = await request.json();
    const others = cart.items.filter((line) => line.isbn !== book.isbn);
    const line = { isbn: book.isbn, title: book.title, unitPrice: book.price, quantity, lineTotal: book.price * quantity };
    const saved = cartView({ ...cart, items: [...others, line].toSorted((a, b) => a.title.localeCompare(b.title)) });
    carts.set(cart.id, saved);
    return HttpResponse.json(saved);
  }),
];
