import { TestbedHarnessEnvironment } from '@angular/cdk/testing/testbed';
import { TestBed } from '@angular/core/testing';
import { MatButtonHarness } from '@angular/material/button/testing';
import { MatDialogHarness } from '@angular/material/dialog/testing';
import { render, screen } from '@testing-library/angular';
import userEvent from '@testing-library/user-event';
import { dune, hobbit } from '../../testing/books';
import { CartStore } from './cart-store';
import { CartSummary } from './cart-summary';

describe('CartSummary removing a book', () => {
  async function openRemoveDialog(): Promise<MatDialogHarness> {
    const { fixture } = await render(CartSummary);
    const cart = TestBed.inject(CartStore);
    cart.add(dune);
    cart.add(hobbit);

    await userEvent.setup().click(await screen.findByRole('button', { name: 'Remove Dune' }));
    return TestbedHarnessEnvironment.documentRootLoader(fixture).getHarness(MatDialogHarness);
  }

  it('removes the book after confirmation', async () => {
    const dialog = await openRemoveDialog();

    expect(await dialog.getTitleText()).toBe('Remove Dune?');
    await (await dialog.getHarness(MatButtonHarness.with({ text: 'Remove' }))).click();

    expect(TestBed.inject(CartStore).lines().map((line) => line.book.title)).toEqual(['The Hobbit']);
    expect(await TestbedHarnessEnvironment.documentRootLoader(TestBed.getLastFixture()).getAllHarnesses(MatDialogHarness)).toHaveLength(0);
  });

  it('keeps the book when the customer changes their mind', async () => {
    const dialog = await openRemoveDialog();

    await (await dialog.getHarness(MatButtonHarness.with({ text: 'Keep it' }))).click();

    expect(TestBed.inject(CartStore).count()).toBe(2);
  });
});
