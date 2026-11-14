const book = { title: "Dune", price: 450 };

console.log(book.title);    // "Dune"
console.log(book["price"]); // 450
console.log(book.isbn);     // undefined

book.price = 405;           // update
book.stock = 12;            // add a new property
delete book.stock;          // delete the property
console.log(book);          // { title: "Dune", price: 405 }
