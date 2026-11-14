interface CatalogItem {
  title: string;
}
interface EBook extends CatalogItem {
  download(): string;
}
// --- example (uses CatalogItem and EBook from example-01) ---

interface ItemHandler {
  handle(item: CatalogItem): void;
}

const ebookOnly: ItemHandler = {
  handle(ebook: EBook) {
    console.log(ebook.download());
  },
};

ebookOnly.handle({ title: "Emma" }); // TypeError: ebook.download is not a function
