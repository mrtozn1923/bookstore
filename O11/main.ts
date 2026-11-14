import { type Book, formatBook } from "./book.ts";

const dune: Book = { title: "Dune", price: 450 };
console.log(formatBook(dune));
