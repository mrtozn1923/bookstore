import { TestBed } from '@angular/core/testing';
import { PricePipe } from './price-pipe';

describe('PricePipe', () => {
  let pipe: PricePipe;

  beforeEach(() => {
    pipe = TestBed.runInInjectionContext(() => new PricePipe());
  });

  it('formats Turkish lira with the Turkish locale', () => {
    expect(pipe.transform(1234.5)).toBe('₺1.234,50');
  });

  it('formats other currencies with their own symbol', () => {
    expect(pipe.transform(1234.5, 'EUR')).toBe('€1.234,50');
  });

  it('shows free books as "Free"', () => {
    expect(pipe.transform(0)).toBe('Free');
  });

  it('rounds to two decimals', () => {
    expect(pipe.transform(0.1 + 0.2)).toBe('₺0,30');
  });
});
