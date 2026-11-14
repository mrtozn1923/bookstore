import { ResolveFn } from '@angular/router';

export const orderTitle: ResolveFn<string> = (route) => `Order ${route.paramMap.get('id')} · Bookstore`;
