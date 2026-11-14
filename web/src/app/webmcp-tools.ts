import { HttpClient } from '@angular/common/http';
import { WebMcpToolDescriptor, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { Cart } from './assistant/cart';
import { BookCardData } from './assistant/ui-blocks';

// Tarayıcıdaki bir agent'a açılan tool'lar; execute Angular'ın injection context'inde çalışır
export const BOOKSTORE_WEBMCP_TOOLS: WebMcpToolDescriptor[] = [
  {
    name: 'search_catalog',
    description: 'Searches the Bookstore catalog by title, author or genre and returns matching books with prices in TRY.',
    inputSchema: {
      type: 'object',
      properties: { query: { type: 'string', description: 'Title, author or genre, for example "crime".' } },
      required: ['query'],
    },
    annotations: { readOnlyHint: true },
    execute: async (args) => {
      const query = String((args as { query?: unknown }).query ?? '');
      const books = await firstValueFrom(inject(HttpClient).get<BookCardData[]>('/catalog/search', { params: { q: query } }));
      return JSON.stringify(books);
    },
  },
  {
    name: 'add_to_cart',
    description: 'Adds one book to the customer\'s cart by ISBN-13. It does not place an order; the customer checks out.',
    inputSchema: {
      type: 'object',
      properties: { isbn: { type: 'string', pattern: '^\\d{13}$' } },
      required: ['isbn'],
    },
    execute: async (args) => {
      // inject() yalnız ilk await'ten önce çalışır; injection context senkron kısımla sınırlı
      const http = inject(HttpClient);
      const cart = inject(Cart);
      const isbn = String((args as { isbn?: unknown }).isbn ?? '');
      // Agent'ın gönderdiği ISBN'e güvenmiyoruz: kitap katalogdan, fiyat sunucudan gelir
      const [book] = await firstValueFrom(http.get<BookCardData[]>('/catalog/search', { params: { isbn } }));
      if (!book) {
        return `No book with ISBN ${isbn}.`;
      }
      cart.add(book);
      return `Added "${book.title}" to the cart. The cart has ${cart.count()} item(s).`;
    },
  },
];
