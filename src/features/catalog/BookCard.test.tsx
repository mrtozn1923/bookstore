import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router';
import { expect, it, vi } from 'vitest';
import { BookCard } from '@/features/catalog/BookCard';
import { testBooks } from '@/test/books';

const dune = testBooks[0];

it('adds the picked number of copies and resets the counter', async () => {
  const user = userEvent.setup();
  const onAdd = vi.fn();
  // BookCard renders a <Link>, which needs a router around it
  render(<MemoryRouter><BookCard book={dune} quantityInCart={0} onAdd={onAdd} /></MemoryRouter>);

  await user.click(screen.getByRole('button', { name: 'More copies of Dune' }));
  await user.click(screen.getByRole('button', { name: 'More copies of Dune' }));
  await user.click(screen.getByRole('button', { name: 'Add 3 to cart' }));

  expect(onAdd).toHaveBeenCalledExactlyOnceWith(dune, 3);
  expect(screen.getByRole('button', { name: 'Add 1 to cart' })).toBeInTheDocument();
  expect(screen.getByRole('button', { name: 'Fewer copies of Dune' })).toBeDisabled();
});
