const books = [
  { isbn: "9780441172719", title: "Dune", price: 450 },
  { isbn: "9780141439587", title: "Emma", price: 320 },
];

type Book = (typeof books)[number];
// { isbn: string; title: string; price: number; }

function formatPrice(book: Book) {
  return `${book.title}: ${book.price} TRY`;
}

console.log(formatPrice(books[1])); // Emma: 320 TRY
