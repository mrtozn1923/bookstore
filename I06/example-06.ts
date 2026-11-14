interface CatalogItem {
  title: string;
}
interface EBook extends CatalogItem {
  download(): string;
}
// --- example (uses CatalogItem and EBook from example-01) ---

interface StrictItemHandler {
  handle: (item: CatalogItem) => void;
}

const strictEBookOnly: StrictItemHandler = {
  // Error: Type '(ebook: EBook) => void' is not assignable to type '(item: CatalogItem) => void'.
  handle: (ebook: EBook) => console.log(ebook.download()),
};
