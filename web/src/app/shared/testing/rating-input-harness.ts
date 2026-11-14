import { BaseHarnessFilters, ComponentHarness, HarnessPredicate, parallel, TestKey } from '@angular/cdk/testing';

export interface RatingInputHarnessFilters extends BaseHarnessFilters {
  label?: string | RegExp;
  value?: number;
}

export class RatingInputHarness extends ComponentHarness {
  static hostSelector = 'app-rating-input';

  private readonly legend = this.locatorFor('legend');
  private readonly stars = this.locatorForAll('input[type="radio"]');

  static with(options: RatingInputHarnessFilters = {}): HarnessPredicate<RatingInputHarness> {
    return new HarnessPredicate(RatingInputHarness, options)
      .addOption('label', options.label, (harness, label) => HarnessPredicate.stringMatches(harness.getLabel(), label))
      .addOption('value', options.value, async (harness, value) => (await harness.getValue()) === value);
  }

  async getLabel(): Promise<string | null> {
    return (await this.legend()).text();
  }

  async getMax(): Promise<number> {
    return (await this.stars()).length;
  }

  async getValue(): Promise<number> {
    const stars = await this.stars();
    const checked = await parallel(() => stars.map((star) => star.getProperty<boolean>('checked')));
    return checked.indexOf(true) + 1;
  }

  async setValue(value: number): Promise<void> {
    const stars = await this.stars();
    if (value < 1 || value > stars.length) {
      throw new Error(`RatingInput has ${stars.length} stars; cannot select ${value}.`);
    }
    await stars[value - 1].click();
  }

  async increase(): Promise<void> {
    await this.pressOnCurrentStar(TestKey.RIGHT_ARROW);
  }

  async decrease(): Promise<void> {
    await this.pressOnCurrentStar(TestKey.LEFT_ARROW);
  }

  async isDisabled(): Promise<boolean> {
    const [first] = await this.stars();
    return first.getProperty<boolean>('disabled');
  }

  private async pressOnCurrentStar(key: TestKey): Promise<void> {
    const stars = await this.stars();
    const current = Math.max((await this.getValue()) - 1, 0);
    await stars[current].sendKeys(key);
  }
}
