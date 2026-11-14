import { Route } from '@playwright/test';
import { Book } from '../src/app/catalog/book';
import { Order, PlaceOrderRequest } from '../src/app/orders/order';
import { dune, emma, hobbit } from '../src/testing/books';

export class MockApi {
  books: Book[] = [dune, emma, hobbit];
  readonly placedOrders: PlaceOrderRequest[] = [];
  readonly unhandled: string[] = [];
  failOrders = false;

  async handle(route: Route): Promise<void> {
    const request = route.request();
    const url = new URL(request.url());
    const method = request.method();

    if (method === 'GET' && url.pathname === '/api/books') {
      const search = url.searchParams.get('search')?.toLowerCase() ?? '';
      return route.fulfill({ json: this.books.filter((book) => book.title.toLowerCase().includes(search)) });
    }
    if (method === 'GET' && /^\/api\/books\/[\d-]+\/rating$/.test(url.pathname)) {
      return route.fulfill({ json: { average: 4.5, count: 12 } });
    }
    if (method === 'POST' && url.pathname === '/api/auth/sign-in') {
      const { email } = request.postDataJSON() as { email: string };
      return route.fulfill({ json: { email, token: 'e2e-token' } });
    }
    if (method === 'POST' && url.pathname === '/api/orders') {
      if (this.failOrders) {
        return route.fulfill({ status: 503, json: { title: 'Service Unavailable', status: 503 } });
      }
      this.placedOrders.push(request.postDataJSON() as PlaceOrderRequest);
      return route.fulfill({ status: 201, json: this.lastOrder() });
    }
    if (method === 'GET' && url.pathname === '/api/orders/42') {
      return route.fulfill({ json: this.lastOrder() });
    }

    this.unhandled.push(`${method} ${url.pathname}${url.search}`);
    return route.fulfill({ status: 404, json: { title: 'Not Found', status: 404 } });
  }

  private lastOrder(): Order {
    const request = this.placedOrders.at(-1);
    const lines = (request?.lines ?? []).map((line) => {
      const book = this.books.find((candidate) => candidate.isbn === line.isbn)!;
      return { isbn: book.isbn, title: book.title, quantity: line.quantity, price: book.price };
    });
    return {
      id: 42,
      reference: 'BS-1042',
      status: 'Placed',
      total: lines.reduce((sum, line) => sum + line.price * line.quantity, 0),
      lines,
      createdAt: '2026-11-14T09:30:00Z',
    };
  }
}
