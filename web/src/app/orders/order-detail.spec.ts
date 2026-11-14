import { Title } from '@angular/platform-browser';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router, withComponentInputBinding } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { screen } from '@testing-library/angular';
import { http, HttpResponse } from 'msw';
import { routes } from '../app.routes';
import { Session } from '../auth/session';
import { server, useMockApi } from '../../testing/msw';

describe('Order detail route', () => {
  useMockApi();

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({ providers: [provideRouter(routes, withComponentInputBinding())] });
  });

  function signIn(): void {
    TestBed.inject(Session).user.set({ email: 'ada@example.com', token: 'test-token' });
  }

  it('shows the order whose id is in the URL', async () => {
    signIn();
    const harness = await RouterTestingHarness.create();

    await harness.navigateByUrl('/orders/42');

    expect(await screen.findByRole('heading', { name: 'Order BS-1042' })).toBeInTheDocument();
    expect(screen.getByRole('list', { name: 'Order lines' })).toHaveTextContent('2 × Dune');
    expect(screen.getByText('Total: ₺1.280,00')).toBeInTheDocument();
  });

  it('sets the page title from the route', async () => {
    signIn();
    const harness = await RouterTestingHarness.create();

    await harness.navigateByUrl('/orders/42');

    expect(TestBed.inject(Title).getTitle()).toBe('Order 42 · Bookstore');
  });

  it('sends a signed-out visitor to sign in and remembers the order', async () => {
    const harness = await RouterTestingHarness.create();

    await harness.navigateByUrl('/orders/42');

    expect(TestBed.inject(Router).url).toBe('/sign-in?returnUrl=%2Forders%2F42');
    expect(await screen.findByRole('heading', { name: 'Sign in' })).toBeInTheDocument();
  });

  it("does not reveal another customer's order", async () => {
    server.use(http.get('/api/orders/:id', () => HttpResponse.json({ title: 'Forbidden', status: 403 }, { status: 403 })));
    signIn();
    const harness = await RouterTestingHarness.create();

    await harness.navigateByUrl('/orders/77');

    expect(await screen.findByRole('alert')).toHaveTextContent('This order could not be found.');
  });
});
