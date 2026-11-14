import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { render, screen } from '@testing-library/angular';
import userEvent, { UserEvent } from '@testing-library/user-event';
import { http, HttpResponse } from 'msw';
import { MockComponent, ngMocks } from 'ng-mocks';
import { dune } from '../../testing/books';
import { server, useMockApi } from '../../testing/msw';
import { placedOrder } from '../../mocks/handlers';
import { CartStore } from '../cart/cart-store';
import { Order, PlaceOrderRequest } from '../orders/order';
import { OrderApi } from '../orders/order-api';
import { CheckoutForm } from './checkout-form';
import { CheckoutPage } from './checkout-page';

const routes = [{ path: 'orders/:id', children: [] }];

async function fillAndSubmit(user: UserEvent): Promise<void> {
  await user.type(screen.getByLabelText('Full name'), 'Ada Lovelace');
  await user.type(screen.getByLabelText('Email'), 'ada@example.com');
  await user.selectOptions(screen.getByLabelText('City'), 'Izmir');
  await user.click(screen.getByRole('button', { name: 'Place order' }));
}

function fillCart(): CartStore {
  const cart = TestBed.inject(CartStore);
  cart.add(dune);
  cart.add(dune);
  return cart;
}

class FakeOrderApi {
  readonly requests: PlaceOrderRequest[] = [];

  place(request: PlaceOrderRequest): Promise<Order> {
    this.requests.push(request);
    return Promise.resolve({ ...placedOrder, id: 100 + this.requests.length });
  }
}

describe('CheckoutPage', () => {
  it('sends the cart lines and opens the new order', async () => {
    const place = vi.fn<OrderApi['place']>().mockResolvedValue(placedOrder);
    await render(CheckoutPage, { providers: [{ provide: OrderApi, useValue: { place } }], routes });
    const cart = fillCart();

    await fillAndSubmit(userEvent.setup());

    expect(place).toHaveBeenCalledExactlyOnceWith({
      fullName: 'Ada Lovelace',
      email: 'ada@example.com',
      city: 'Izmir',
      lines: [{ isbn: dune.isbn, quantity: 2 }],
    });
    expect(cart.lines()).toEqual([]);
    expect(TestBed.inject(Router).url).toBe('/orders/42');
  });

  it('keeps the cart and shows an error when the order fails', async () => {
    await render(CheckoutPage, { routes });
    const cart = fillCart();
    vi.spyOn(TestBed.inject(OrderApi), 'place').mockRejectedValue(new Error('503'));

    await fillAndSubmit(userEvent.setup());

    expect(await screen.findByRole('alert')).toHaveTextContent('We could not place your order. Please try again.');
    expect(cart.count()).toBe(2);
    expect(TestBed.inject(Router).url).toBe('/');
  });

  it('opens every new order it places', async () => {
    await render(CheckoutPage, { providers: [{ provide: OrderApi, useClass: FakeOrderApi }], routes });
    const fake = TestBed.inject(OrderApi) as unknown as FakeOrderApi;
    const user = userEvent.setup();
    fillCart();

    await fillAndSubmit(user);
    expect(TestBed.inject(Router).url).toBe('/orders/101');

    fillCart();
    await user.click(screen.getByRole('button', { name: 'Place order' }));
    expect(TestBed.inject(Router).url).toBe('/orders/102');
    expect(fake.requests).toHaveLength(2);
  });
});

describe('CheckoutPage against the network', () => {
  useMockApi();

  it('posts the order as JSON', async () => {
    let body: unknown;
    server.use(
      http.post('/api/orders', async ({ request }) => {
        body = await request.json();
        return HttpResponse.json(placedOrder, { status: 201 });
      }),
    );
    await render(CheckoutPage, { routes });
    fillCart();

    await fillAndSubmit(userEvent.setup());

    await vi.waitFor(() => expect(TestBed.inject(Router).url).toBe('/orders/42'));
    expect(body).toEqual({ fullName: 'Ada Lovelace', email: 'ada@example.com', city: 'Izmir', lines: [{ isbn: dune.isbn, quantity: 2 }] });
  });
});

describe('CheckoutPage with the form mocked', () => {
  it('opens the order when the form emits', async () => {
    const place = vi.fn<OrderApi['place']>().mockResolvedValue(placedOrder);
    await render(CheckoutPage, {
      importOverrides: [{ replace: CheckoutForm, with: MockComponent(CheckoutForm) }],
      providers: [{ provide: OrderApi, useValue: { place } }],
      routes,
    });

    ngMocks.findInstance(CheckoutForm).placeOrder.emit({ fullName: 'Ada Lovelace', email: 'ada@example.com', city: 'Izmir' });

    await vi.waitFor(() => expect(TestBed.inject(Router).url).toBe('/orders/42'));
    expect(screen.queryByLabelText('Email')).not.toBeInTheDocument();
  });
});
