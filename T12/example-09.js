const prices = { dune: 450, emma: 320, hobbit: 390 };

for (const key in prices) {
  console.log(`${key}: ${prices[key]}`);
}
// dune: 450
// emma: 320
// hobbit: 390

for (const entry of Object.entries(prices)) {
  console.log(entry[0], entry[1]);
}
