import type { BookDto, OrderDto } from '../api/book-api.service';

export function stockLabel(book: BookDto): string {
  return book.stock > 0 ? `${book.stock} in stock` : 'Out of stock';
}

export function statusLabel(order: OrderDto): string {
  switch (order.status) {
    case 'Placed':
      return 'Order placed';
    case 'Cancelled':
      return 'Order cancelled';
    default: {
      const unhandled: never = order.status;
      return unhandled;
    }
  }
}
