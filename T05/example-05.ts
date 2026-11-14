const entry: [string, number] = ["Dune", 450];

entry[0].toUpperCase();
entry[1].toFixed(2);
// Error: Tuple type '[string, number]' of length '2' has no element at index '2'.
const extra = entry[2];
// Error: the type doesn't match in either position (TS2322).
const swapped: [string, number] = [450, "Dune"];
