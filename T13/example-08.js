const text = '{"title":"Emma","price":320,"inStock":true}';
const book = JSON.parse(text);

console.log(book.title);                     // "Emma"
console.log(book.price + 40);                // 360
console.log(JSON.parse("[1, 2, 3]").length); // 3
