function printAll(items: readonly string[]) {
  for (const item of items) {
    console.log(item);
  }
}

function sortInPlace(items: string[]) {
  items.sort();
}

const cartTitles = ["Dune", "Emma"];
const bestsellers: readonly string[] = ["The Hobbit", "Middlemarch"];

printAll(cartTitles);
printAll(bestsellers);
// Error: The type 'readonly string[]' is 'readonly' and cannot be assigned to the mutable type 'string[]'.
sortInPlace(bestsellers);
