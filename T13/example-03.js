const book = { title: "Dune", price: 450, categories: ["science-fiction", "classics"] };
const json = JSON.stringify(book);

console.log(json);        // {"title":"Dune","price":450,"categories":["science-fiction","classics"]}
console.log(typeof json); // "string"
