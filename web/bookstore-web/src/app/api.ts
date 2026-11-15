import { InjectionToken } from '@angular/core';

// Bookstore.Web'in adresi; SSR sunucusu API'ye bu adresten gider
export const API_BASE = new InjectionToken<string>('API_BASE', { factory: () => 'http://127.0.0.1:52213' });
