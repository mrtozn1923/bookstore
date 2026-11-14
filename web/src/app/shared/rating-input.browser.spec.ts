import { render } from '@testing-library/angular';
import { page } from 'vitest/browser';
import { RatingInput } from './rating-input';

describe('RatingInput appearance', () => {
  it('fills the stars up to the selected rating', async () => {
    await render(RatingInput, { inputs: { label: 'Rate the book', value: 3 } });

    await expect.element(page.getByRole('group', { name: 'Rate the book' })).toMatchScreenshot('three-of-five');
  });
});
