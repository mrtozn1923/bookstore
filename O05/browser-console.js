// O05: examples that need a browser; paste these blocks into the DevTools console one by one
// (if a block uses a function from examples.js, paste that file first)

// --- example 5 ---
const catalog = [
  { title: "Emma", author: "Jane Austen" },
  { title: "Dune", author: "Frank Herbert" },
  { title: "Pride and Prejudice", author: "Jane Austen" }
];

// The classic way
const byAuthorOld = new Map();
for (const book of catalog) {
  if (!byAuthorOld.has(book.author)) byAuthorOld.set(book.author, []);
  byAuthorOld.get(book.author).push(book.title);
}

// ES2026
const byAuthor = new Map();
for (const book of catalog) {
  byAuthor.getOrInsert(book.author, []).push(book.title);
}
console.log(byAuthor); // Map(2) { "Jane Austen" => ["Emma", "Pride and Prejudice"], "Frank Herbert" => ["Dune"] }

// --- example 6 ---
const slugCache = new Map();

function toSlug(title) {
  return slugCache.getOrInsertComputed(title, (t) => {
    console.log("computing:", t);
    return t.toLowerCase().replaceAll(" ", "-");
  });
}

console.log(toSlug("Pride and Prejudice")); // "computing: Pride and Prejudice", then "pride-and-prejudice"
console.log(toSlug("Pride and Prejudice")); // "pride-and-prejudice", from the cache
