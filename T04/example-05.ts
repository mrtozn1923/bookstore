const titles = ["Dune", "Emma"];
const found = titles.find((title) => title.startsWith("Middle"));

// Error: 'found' is possibly 'undefined'.
console.log(found.toUpperCase());

if (found !== undefined) {
  console.log(found.toUpperCase());
}

console.log(found?.toUpperCase());
// undefined
