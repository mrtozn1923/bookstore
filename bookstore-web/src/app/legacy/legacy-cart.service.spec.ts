import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';
import type { Book } from '../models';
import { LegacyCartService } from './legacy-cart.service';

describe('LegacyCartService', () => {
  it('counts copies through an Observable', async () => {
    const cart = TestBed.inject(LegacyCartService);
    const dune = { isbn: '9780441172719', title: 'Dune', price: 450 } as Book;
    cart.add(dune);
    cart.add(dune);
    // every read is a subscription: here a one-off firstValueFrom, in a template the async pipe
    expect(await firstValueFrom(cart.count$)).toBe(2);
  });
});
