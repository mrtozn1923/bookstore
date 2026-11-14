interface ApiList<T> {
  items: T[];
  total: number;
  page: number;
}

interface Book {
  isbn: string;
  title: string;
  price: number;
}

const books: ApiList<Book> = {
  items: [{ isbn: "9780441172719", title: "Dune", price: 450 }],
  total: 1,
  page: 1,
};

const titles = books.items.map((book) => book.title); // string[]
