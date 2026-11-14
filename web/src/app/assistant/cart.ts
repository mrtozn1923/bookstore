import { Injectable, computed, signal } from '@angular/core';
import { BookCardData } from './ui-blocks';

@Injectable({ providedIn: 'root' })
export class Cart {
  readonly items = signal<BookCardData[]>([]);
  readonly count = computed(() => this.items().length);

  // Sepete yalnız kullanıcının tıklaması ekler; model hiçbir zaman doğrudan çağırmaz
  add(book: BookCardData): void {
    this.items.update((list) => [...list, book]);
  }
}
