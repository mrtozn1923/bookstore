type Timestamps = {
  createdAt: Date;
  updatedAt: Date;
};

type Book = {
  isbn: string;
  title: string;
  price: number;
};

type StoredBook = Book & Timestamps;

function save(book: Book): StoredBook {
  const now = new Date();
  return { ...book, createdAt: now, updatedAt: now };
}

const saved = save({ isbn: "9780441172719", title: "Dune", price: 450 });
console.log(saved.title, saved.createdAt instanceof Date); // Dune true
