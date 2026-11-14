import books from "./books.json" with { type: "json" };

console.log(books.length, "books, first:", books[0].title);
