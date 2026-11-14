import { TestbedHarnessEnvironment } from '@angular/cdk/testing/testbed';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { RatingInput } from './rating-input';
import { RatingInputHarness } from './testing/rating-input-harness';

describe('RatingInput', () => {
  let fixture: ComponentFixture<RatingInput>;

  async function create(): Promise<RatingInputHarness> {
    fixture = TestBed.createComponent(RatingInput);
    fixture.componentRef.setInput('label', 'Rate the book');
    return TestbedHarnessEnvironment.harnessForFixture(fixture, RatingInputHarness);
  }

  it('shows five empty stars by default', async () => {
    const rating = await create();

    expect(await rating.getMax()).toBe(5);
    expect(await rating.getValue()).toBe(0);
  });

  it('selects the clicked star', async () => {
    const rating = await create();

    await rating.setValue(4);

    expect(await rating.getValue()).toBe(4);
    expect(fixture.componentInstance.value()).toBe(4);
  });

  it('moves with the arrow keys and stops at the last star', async () => {
    const rating = await create();
    await rating.setValue(4);

    await rating.increase();
    await rating.increase();
    expect(await rating.getValue()).toBe(5);

    await rating.decrease();
    expect(await rating.getValue()).toBe(4);
  });

  it('cannot be changed when disabled', async () => {
    const rating = await create();
    fixture.componentRef.setInput('disabled', true);

    expect(await rating.isDisabled()).toBe(true);
    await rating.setValue(3);
    expect(await rating.getValue()).toBe(0);
  });
});
