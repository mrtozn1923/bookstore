function longest<T extends { length: number }>(a: T, b: T): T {
  return a.length >= b.length ? a : b;
}

const titles = longest(["Dune"], ["Emma", "The Hobbit"]);
const price = longest(450, 320);
