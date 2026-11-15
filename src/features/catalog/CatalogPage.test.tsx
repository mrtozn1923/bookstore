import { screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { expect, it } from 'vitest';
import { renderApp } from '@/test/render';

it('searches the catalog, keeps the search in the URL and adds a book to the cart', async () => {
  const user = userEvent.setup();
  const router = renderApp('/');

  // findBy* waits (1 s by default) until the API answer has been rendered
  expect(await screen.findByText('4 book(s) found')).toBeInTheDocument();

  await user.type(screen.getByRole('searchbox', { name: 'Search books' }), 'austen');

  expect(await screen.findByText('2 book(s) found')).toBeInTheDocument();
  expect(router.state.location.search).toBe('?q=austen');
  const titles = within(screen.getByRole('list')).getAllByRole('link').map((link) => link.textContent);
  expect(titles).toEqual(['Emma', 'Pride and Prejudice']);

  await user.click(screen.getAllByRole('button', { name: 'Add 1 to cart' })[0]);
  expect(await screen.findByText(/Cart: 1 item\(s\)/)).toBeInTheDocument();
});
