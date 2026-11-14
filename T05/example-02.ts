const titles: string[] = ["Dune", "Emma"];
const prices: Array<number> = [450, 320, 390];

// Error: Argument of type 'number' is not assignable to parameter of type 'string'.
titles.push(450);
