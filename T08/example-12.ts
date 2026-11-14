const mixed: (string | number)[] = [450, "Dune", 320];

// Error: Type '(string | number)[]' is not assignable to type 'string[] | number[]'.
const uniform: string[] | number[] = [450, "Dune", 320];
