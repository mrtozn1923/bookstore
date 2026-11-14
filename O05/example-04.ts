function createCartLine(isbn: string, quantity: number) {
  return { id: Date.now(), isbn, quantity };
}

type CreateCartLine = typeof createCartLine;
// (isbn: string, quantity: number) => { id: number; isbn: string; quantity: number; }

const createGiftLine: CreateCartLine = (isbn, quantity) => ({ id: 0, isbn, quantity });
