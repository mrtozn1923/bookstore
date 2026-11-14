import { render, screen } from '@testing-library/angular';
import { page } from 'vitest/browser';
import { useMockApiInBrowser } from '../../testing/msw-browser';
import { CatalogPage } from './catalog-page';

describe('CatalogPage in a real browser', () => {
  useMockApiInBrowser();

  it('shows every book after scrolling to the end on a large screen', async () => {
    await page.viewport(1920, 1080);
    await render(CatalogPage);
    expect(await screen.findAllByRole('heading', { level: 3 })).not.toHaveLength(0);

    window.scrollTo(0, document.body.scrollHeight);

    await expect.poll(() => screen.getAllByRole('heading', { level: 3 }).length).toBe(30);
  });

  it('keeps the cart status visible while the list scrolls', async () => {
    await page.viewport(800, 600);
    await render(CatalogPage);
    await screen.findAllByRole('article');

    window.scrollTo(0, 1500);

    const cartStatus = page.getByRole('complementary', { name: 'Cart status' });
    await expect.element(cartStatus).toBeInViewport();
    expect(cartStatus.element().getBoundingClientRect().top).toBe(0);
  });
});
