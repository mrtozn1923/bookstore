import { Book, describe } from "./book-types.ts";

const hobbit: Book = { isbn: "9780547928227", title: "The Hobbit" };
console.log(describe(hobbit));
