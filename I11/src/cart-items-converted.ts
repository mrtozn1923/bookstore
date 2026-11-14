interface Book {
  isbn: string;
  title: string;
  price: number;
}

interface CartItem {
  book: Book;
  quantity: number;
}

const items: CartItem[] = [];

export function addToCart(book: Book, quantity = 1) {
  items.push({ book, quantity });
}

export function last<T>(list: T[]): T | undefined {
  return list[list.length - 1];
}
