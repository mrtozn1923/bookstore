type Box<T> = { value: T };

const price: Box<number> = { value: 450 };
const title: Box<string> = { value: "Dune" };

// Error: Generic type 'Box' requires 1 type argument(s).
const broken: Box = { value: true };
