function longestPlain(a: { length: number }, b: { length: number }) {
  return a.length >= b.length ? a : b;
}

const titles = longestPlain(["Dune"], ["Emma", "The Hobbit"]); // { length: number; }
titles.join(", "); // Error: Property 'join' does not exist on type '{ length: number; }'.
