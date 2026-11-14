const text = `{
  "title": "Dune",
  "price": 450,
  "inStock": true,
  "categories": ["science-fiction", "classics"],
  "author": { "name": "Frank Herbert" },
  "subtitle": null
}`;

const book = JSON.parse(text);
console.log(book.author.name); // "Frank Herbert"
