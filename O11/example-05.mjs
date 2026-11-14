const response = await fetch("https://httpbin.org/anything/books", {
  method: "POST",
  headers: {
    "Content-Type": "application/json",
  },
  body: JSON.stringify({
    isbn: "9780441172719",
    title: "Dune",
    price: 450,
  }),
});

console.log(response.status); // 200
const echo = await response.json();
console.log(echo.method, echo.json);
// POST { isbn: "9780441172719", price: 450, title: "Dune" }
