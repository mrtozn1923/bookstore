interface Book {
  isbn: string;
  title: string;
  author: string;
}

type BookKey = keyof Book; // "isbn" | "title" | "author"

const sortBy: BookKey = "title";
// Error: Type '"publisher"' is not assignable to type 'keyof Book'.
const filterBy: BookKey = "publisher";
