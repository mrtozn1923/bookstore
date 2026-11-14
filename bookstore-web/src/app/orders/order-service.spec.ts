import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { apiUrl } from '../api';
import { OrderService, type OrderStatus } from './order-service';

describe('OrderService.status$', () => {
  let service: OrderService;
  let httpTesting: HttpTestingController;
  const statusUrl = `${apiUrl}/orders/1042/status`;

  beforeEach(() => {
    // timer(0, 5000) runs on setInterval, so fake timers move the polling forward
    vi.useFakeTimers();
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(OrderService);
    httpTesting = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpTesting.verify();
    vi.useRealTimers();
  });

  it('polls every 5 seconds and stops after Delivered', () => {
    const statuses: string[] = [];
    let completed = false;
    service.status$(1042).subscribe({
      next: (order) => statuses.push(order.status),
      complete: () => (completed = true),
    });

    vi.advanceTimersByTime(0);
    httpTesting.expectOne(statusUrl).flush({ id: 1042, status: 'Paid' } satisfies OrderStatus);
    vi.advanceTimersByTime(5000);
    httpTesting.expectOne(statusUrl).flush({ id: 1042, status: 'Shipped' } satisfies OrderStatus);
    vi.advanceTimersByTime(5000);
    httpTesting.expectOne(statusUrl).flush({ id: 1042, status: 'Delivered' } satisfies OrderStatus);

    // after Delivered: no more requests, however long we wait
    vi.advanceTimersByTime(60_000);
    httpTesting.expectNone(statusUrl);
    expect(statuses).toEqual(['Paid', 'Shipped', 'Delivered']);
    expect(completed).toBe(true);
  });

  it('never has two requests in flight when the API is slow', () => {
    service.status$(1042).subscribe();

    vi.advanceTimersByTime(0);
    const first = httpTesting.expectOne(statusUrl);
    // the answer takes 12 seconds: two ticks of the timer pass meanwhile
    vi.advanceTimersByTime(12_000);
    httpTesting.expectNone(statusUrl);
    first.flush({ id: 1042, status: 'Paid' } satisfies OrderStatus);

    // exhaustMap ignored the ticks at 5 s and 10 s; the next request comes at 15 s
    vi.advanceTimersByTime(3000);
    httpTesting.expectOne(statusUrl).flush({ id: 1042, status: 'Delivered' } satisfies OrderStatus);
  });
});
