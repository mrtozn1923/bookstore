import { DEFAULT_CURRENCY_CODE, LOCALE_ID, Provider } from '@angular/core';

export const localeProviders: Provider[] = [
  { provide: LOCALE_ID, useValue: 'tr' },
  { provide: DEFAULT_CURRENCY_CODE, useValue: 'TRY' },
];
