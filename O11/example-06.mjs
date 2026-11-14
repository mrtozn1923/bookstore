// Replace the whole record
const updated = await fetch("https://httpbin.org/anything/books/9780441172719", {
  method: "PUT",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify({ isbn: "9780441172719", title: "Dune", price: 420 }),
});
console.log(updated.status); // 200

// Delete the record
const deleted = await fetch("https://httpbin.org/anything/books/9780441172719", {
  method: "DELETE",
});
console.log(deleted.status); // 200
