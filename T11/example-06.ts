type WithStringIsbn = { isbn: string };
type WithNumberIsbn = { isbn: number };

type Broken = WithStringIsbn & WithNumberIsbn;

// Error: Type 'string' is not assignable to type 'never'.
const book: Broken = { isbn: "9780441172719" };
