// URLPattern is a global since Node 24: route matching without a framework
const bookRoute = new URLPattern({ pathname: "/api/books/:isbn(97[89]\\d{10})" });

console.log(bookRoute.test("http://127.0.0.1:3000/api/books/9780441172719"));
console.log(bookRoute.test("http://127.0.0.1:3000/api/books/dune"));
console.log(bookRoute.exec("http://127.0.0.1:3000/api/books/9780441172719?format=json")?.pathname.groups);
