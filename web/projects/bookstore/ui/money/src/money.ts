import { Pipe, PipeTransform } from '@angular/core';

export interface Money {
  amount: number;
  currency: string;
}

@Pipe({ name: 'money' })
export class MoneyPipe implements PipeTransform {
  transform(value: Money | null | undefined, locale = 'en-US'): string {
    if (!value) {
      return '';
    }
    return new Intl.NumberFormat(locale, { style: 'currency', currency: value.currency }).format(value.amount);
  }
}
