import { DeferBlockBehavior } from '@angular/core/testing';
import { render, screen, within } from '@testing-library/angular';
import { page } from 'vitest/browser';
import { useMockApiInBrowser } from '../../testing/msw-browser';
import { CatalogPage } from './catalog-page';

describe('BookCard ratings in a real browser', () => {
  useMockApiInBrowser();

  it('loads the ratings of a card only when it scrolls into view', async () => {
    await page.viewport(800, 600);
    await render(CatalogPage, { deferBlockBehavior: DeferBlockBehavior.Playthrough });
    const cards = await screen.findAllByRole('article');

    expect(await within(cards[0]).findByText('4.5 / 5 (12 ratings)')).toBeInTheDocument();
    expect(cards[11]).not.toHaveTextContent('12 ratings');

    cards[11].scrollIntoView();

    expect(await within(cards[11]).findByText('4.5 / 5 (12 ratings)')).toBeInTheDocument();
  });
});
