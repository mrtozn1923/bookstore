// Shared sample data (Bookstore, the site's shared example app). Prices in TRY.
export interface Book {
  isbn: string;
  title: string;
  author: string;
  price: number;
  stock: number;
}

export const books: Book[] = [
  { isbn: "9780441172719", title: "Dune", author: "Frank Herbert", price: 450, stock: 12 },
  { isbn: "9780141439587", title: "Emma", author: "Jane Austen", price: 320, stock: 8 },
  { isbn: "9780547928227", title: "The Hobbit", author: "J. R. R. Tolkien", price: 390, stock: 5 },
  { isbn: "9780141439549", title: "Middlemarch", author: "George Eliot", price: 380, stock: 3 },
  { isbn: "9780141439471", title: "Frankenstein", author: "Mary Shelley", price: 280, stock: 0 },
  { isbn: "9780141439518", title: "Pride and Prejudice", author: "Jane Austen", price: 300, stock: 7 },
];

export const customers = ["Ada", "Alan", "Grace", "Linus"];
