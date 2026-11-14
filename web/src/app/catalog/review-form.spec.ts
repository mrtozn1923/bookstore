import { HarnessLoader } from '@angular/cdk/testing';
import { TestbedHarnessEnvironment } from '@angular/cdk/testing/testbed';
import { render, screen, within } from '@testing-library/angular';
import userEvent from '@testing-library/user-event';
import { RatingInputHarness } from '../shared/testing/rating-input-harness';
import { Review, ReviewForm } from './review-form';

describe('ReviewForm', () => {
  async function setup(): Promise<{ loader: HarnessLoader; send: ReturnType<typeof vi.fn<(review: Review) => void>> }> {
    const send = vi.fn<(review: Review) => void>();
    const { fixture } = await render(ReviewForm, { on: { send } });
    return { loader: TestbedHarnessEnvironment.loader(fixture), send };
  }

  it('sends both ratings and the comment', async () => {
    const { loader, send } = await setup();
    const user = userEvent.setup();

    await (await loader.getHarness(RatingInputHarness.with({ label: 'Rate the book' }))).setValue(4);
    await (await loader.getHarness(RatingInputHarness.with({ label: 'Rate the delivery' }))).setValue(5);
    await user.type(screen.getByLabelText('Comment'), 'Arrived well packed.');
    await user.click(screen.getByRole('button', { name: 'Send review' }));

    expect(send).toHaveBeenCalledExactlyOnceWith({ book: 4, delivery: 5, comment: 'Arrived well packed.' });
  });

  it('asks for the missing rating', async () => {
    const { loader, send } = await setup();

    await (await loader.getHarness(RatingInputHarness.with({ label: 'Rate the book' }))).setValue(4);
    await userEvent.setup().click(screen.getByRole('button', { name: 'Send review' }));

    const unrated = await loader.getAllHarnesses(RatingInputHarness.with({ value: 0 }));
    expect(await Promise.all(unrated.map((rating) => rating.getLabel()))).toEqual(['Rate the delivery']);
    expect(await screen.findByText('Rate the delivery.')).toBeInTheDocument();
    expect(screen.queryByText('Rate the book.')).not.toBeInTheDocument();
    expect(send).not.toHaveBeenCalled();
  });

  it('can also be rated with role queries', async () => {
    const { send } = await setup();
    const user = userEvent.setup();

    await user.click(within(screen.getByRole('group', { name: 'Rate the book' })).getByRole('radio', { name: '4 stars' }));
    await user.click(within(screen.getByRole('group', { name: 'Rate the delivery' })).getByRole('radio', { name: '5 stars' }));
    await user.click(screen.getByRole('button', { name: 'Send review' }));

    expect(send).toHaveBeenCalledExactlyOnceWith({ book: 4, delivery: 5, comment: '' });
  });
});
