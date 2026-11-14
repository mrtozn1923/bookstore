interface CatalogItem {
  title: string;
}
interface EBook extends CatalogItem {
  download(): string;
}
// --- example (uses CatalogItem and EBook from example-01) ---

let getEBook = (): EBook => ({ title: "Dune", download: () => "dune.epub" });
let getItem = (): CatalogItem => ({ title: "Emma" });

getItem = getEBook;

// Error: Type '() => CatalogItem' is not assignable to type '() => EBook'.
getEBook = getItem;
