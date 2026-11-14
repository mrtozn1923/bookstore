interface CatalogItem {
  title: string;
}
interface EBook extends CatalogItem {
  download(): string;
}

let handleItem = (item: CatalogItem) => console.log(item.title);
let handleEBook = (ebook: EBook) => console.log(ebook.download());

handleEBook = handleItem;
handleItem = handleEBook;
