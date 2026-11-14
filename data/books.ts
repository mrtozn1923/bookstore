// Sample data for the mock Bookstore API (Bookstore, the site's shared example app).
// Prices are in TRY; ISBN-13 values have valid check digits.

export interface Book {
  isbn: string;
  title: string;
  author: string;
  price: number;
  stock: number;
  category: string;
}

export interface Review {
  id: number;
  isbn: string;
  author: string;
  rating: number;
  text: string;
}

export interface Coupon {
  code: string;
  percent: number;
}

export const categories = [
  { slug: "science-fiction", name: "Science Fiction" },
  { slug: "classics", name: "Classics" },
  { slug: "fantasy", name: "Fantasy" },
];

export const books: Book[] = [
  { isbn: "9780441172719", title: "Dune", author: "Frank Herbert", price: 450, stock: 12, category: "science-fiction" },
  { isbn: "9780141439587", title: "Emma", author: "Jane Austen", price: 320, stock: 8, category: "classics" },
  { isbn: "9780547928227", title: "The Hobbit", author: "J. R. R. Tolkien", price: 390, stock: 5, category: "fantasy" },
  { isbn: "9780141439549", title: "Middlemarch", author: "George Eliot", price: 380, stock: 3, category: "classics" },
  { isbn: "9780141439471", title: "Frankenstein", author: "Mary Shelley", price: 280, stock: 0, category: "classics" },
  { isbn: "9780141439518", title: "Pride and Prejudice", author: "Jane Austen", price: 300, stock: 7, category: "classics" },
];

export const reviews: Review[] = [
  { id: 1, isbn: "9780441172719", author: "Ada", rating: 5, text: "Still the best world-building I know." },
  { id: 2, isbn: "9780441172719", author: "Alan", rating: 4, text: "Slow start, great second half." },
  { id: 3, isbn: "9780141439587", author: "Grace", rating: 4, text: "Funny and sharp." },
];

export const coupons: Coupon[] = [{ code: "BOOKWEEK", percent: 10 }];
