import { formatCurrency, getCurrencySymbol } from '@angular/common';
import { inject, LOCALE_ID, Pipe, PipeTransform } from '@angular/core';

@Pipe({ name: 'price' })
export class PricePipe implements PipeTransform {
  private readonly locale = inject(LOCALE_ID);

  transform(value: number, currency = 'TRY'): string {
    if (value === 0) {
      return 'Free';
    }
    return formatCurrency(value, this.locale, getCurrencySymbol(currency, 'narrow', this.locale), currency);
  }
}
