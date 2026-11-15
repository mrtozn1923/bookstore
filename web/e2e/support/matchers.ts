import { expect as baseExpect, type ExpectMatcherState, type Locator } from '@playwright/test';

// Uygulamanın para biçimiyle aynı: Angular'daki currency pipe (en-US, TRY, symbol-narrow)
const money = new Intl.NumberFormat('en-US', { style: 'currency', currency: 'TRY', currencyDisplay: 'narrowSymbol' });

export function formatPrice(amount: number): string {
  return money.format(amount);
}

export const priceMatchers = {
  // Web-first kalır: içerideki toContainText, timeout dolana kadar tekrar dener
  async toShowPrice(this: ExpectMatcherState, locator: Locator, amount: number, options?: { timeout?: number }) {
    const expected = formatPrice(amount);
    const assertion = this.isNot ? baseExpect(locator).not : baseExpect(locator);
    try {
      await assertion.toContainText(expected, { timeout: options?.timeout ?? this.timeout });
      return { pass: !this.isNot, name: 'toShowPrice', message: () => `${locator} shows ${expected}` };
    } catch (error) {
      const actual = (error as { matcherResult?: { actual?: unknown } }).matcherResult?.actual;
      return {
        pass: this.isNot,
        name: 'toShowPrice',
        expected,
        actual,
        message: () =>
          `${this.utils.matcherHint('toShowPrice', undefined, undefined, { isNot: this.isNot })}\n\n` +
          `Locator: ${locator}\n` +
          `Expected: ${this.isNot ? 'not ' : ''}${this.utils.printExpected(expected)}\n` +
          `Received: ${this.utils.printReceived(actual)}`,
      };
    }
  },
};
