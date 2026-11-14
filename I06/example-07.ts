interface CatalogItem {
  title: string;
}
interface EBook extends CatalogItem {
  download(): string;
}
// --- example (uses CatalogItem and EBook from example-01) ---

type Box<T> = {
  get: () => T;
  set: (value: T) => void;
};

declare let itemBox: Box<CatalogItem>;
declare let ebookBox: Box<EBook>;

// Error: Type 'Box<EBook>' is not assignable to type 'Box<CatalogItem>'.
itemBox = ebookBox;

// Error: Type 'Box<CatalogItem>' is not assignable to type 'Box<EBook>'.
ebookBox = itemBox;
