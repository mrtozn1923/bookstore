type Book = { title: string; price: number };

const raw = '{"title":"Dune","price":450}';
const book = JSON.parse(raw) as Book;
console.log(book.title); // Dune
