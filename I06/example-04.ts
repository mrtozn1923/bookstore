interface CatalogItem {
  title: string;
}
interface EBook extends CatalogItem {
  download(): string;
}
// --- example (uses CatalogItem and EBook from example-01) ---

let handleItem = (item: CatalogItem) => console.log(item.title);
let handleEBook = (ebook: EBook) => console.log(ebook.download());

// Error: Type '(ebook: EBook) => void' is not assignable to type '(item: CatalogItem) => void'.
handleItem = handleEBook;

// With strictFunctionTypes off, the line above compiles and this call crashes:
// TypeError: ebook.download is not a function
handleItem({ title: "Emma" });
