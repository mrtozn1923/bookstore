interface CatalogItem {
  title: string;
}
interface EBook extends CatalogItem {
  download(): string;
}
// --- example (uses CatalogItem and EBook from example-01) ---

interface PrintedBook extends CatalogItem {
  ship(): string;
}

const ebooks: EBook[] = [{ title: "Dune", download: () => "dune.epub" }];
const items: CatalogItem[] = ebooks;
const emma: PrintedBook = { title: "Emma", ship: () => "Shipping Emma" };
items.push(emma);

for (const ebook of ebooks) {
  console.log(ebook.download());
}
// dune.epub
// TypeError: ebook.download is not a function
