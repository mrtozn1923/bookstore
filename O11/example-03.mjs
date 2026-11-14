const response = await fetch("https://openlibrary.org/isbn/9780441172719.json");

console.log(response.status);                       // 200
console.log(response.ok);                           // true
console.log(response.headers.get("content-type")); // "application/json"
