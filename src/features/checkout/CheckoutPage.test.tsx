import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { http, HttpResponse } from 'msw';
import { expect, it } from 'vitest';
import { useClientStore } from '@/shared/clientStore';
import { carts } from '@/test/handlers';
import { renderApp } from '@/test/render';
import { server } from '@/test/server';

it('shows the API validation error next to the email field', async () => {
  // Arrange the state the route expects: a signed-in user and a cart with one line
  server.use(http.get('/bff/user', () => HttpResponse.json({ name: 'Ada Lovelace', email: 'ada@example.com' })));
  carts.set('cart-1', { id: 'cart-1', currency: 'TRY', total: 489,
    items: [{ isbn: '9780441172719', title: 'Dune', unitPrice: 489, quantity: 1, lineTotal: 489 }] });
  useClientStore.getState().setCartId('cart-1');
  // The same 400 Problem Details body Bookstore.Api sent in post 10
  server.use(http.post('/api/orders', () => HttpResponse.json({
    title: 'One or more validation errors occurred.', status: 400,
    errors: { CustomerEmail: ['The CustomerEmail field is not a valid e-mail address.'] },
  }, { status: 400, headers: { 'Content-Type': 'application/problem+json' } })));
  const user = userEvent.setup();
  renderApp('/checkout');

  await user.type(await screen.findByLabelText('Street and number'), 'Main Street 1');
  await user.type(screen.getByLabelText('City'), 'Ankara');
  await user.type(screen.getByLabelText('Postal code'), '06100');
  await user.click(screen.getByRole('button', { name: 'Place order' }));

  const email = screen.getByLabelText('Email');
  expect(await screen.findByText('The CustomerEmail field is not a valid e-mail address.')).toBeInTheDocument();
  expect(email).toHaveAttribute('aria-invalid', 'true');
  expect(email).toHaveAccessibleDescription('The CustomerEmail field is not a valid e-mail address.');
  expect(email).toHaveFocus();
});

it('sends a signed-out user to the sign-in page and remembers where they wanted to go', async () => {
  const router = renderApp('/checkout');

  expect(await screen.findByRole('heading', { name: 'Sign in' })).toBeInTheDocument();
  expect(router.state.location.pathname + router.state.location.search).toBe('/sign-in?redirectTo=%2Fcheckout');
});
