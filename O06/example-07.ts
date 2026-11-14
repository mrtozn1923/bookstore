type CartEntry = [isbn: string, quantity: number];

type First = CartEntry[0];       // string
type Size = CartEntry["length"]; // 2

// Error: Tuple type 'CartEntry' of length '2' has no element at index '2'.
type Third = CartEntry[2];
