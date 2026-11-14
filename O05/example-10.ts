function pluck<T, K extends keyof T>(items: T[], key: K) {
  return items.map((item) => item[key]);
}

const books = [
  { title: "Dune", price: 450 },
  { title: "Emma", price: 320 },
];

const titles = pluck(books, "title"); // string[]
const prices = pluck(books, "price"); // number[]
console.log(titles, prices); // [ 'Dune', 'Emma' ] [ 450, 320 ]
