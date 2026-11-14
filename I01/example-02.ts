// In JavaScript, on values: condition ? a : b
const stock = 12;
const label = stock > 10 ? "in stock" : "low stock";

// In TypeScript, on types: T extends U ? X : Y
type Label<T> = T extends string ? "text" : "other";

type L1 = Label<"Dune">; // "text"
type L2 = Label<450>; // "other"
