const response = await fetch("https://openlibrary.org/isbn/9780441172719.json");
const book = await response.json();

console.log(book.title);
console.log(book);
