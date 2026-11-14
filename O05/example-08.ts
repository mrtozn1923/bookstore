type StockByIsbn = { [isbn: string]: number };

type StockKey = keyof StockByIsbn; // string | number

const a: StockKey = "9780441172719";
const b: StockKey = 7;
// Error: Type 'boolean' is not assignable to type 'string | number'.
const c: StockKey = true;
