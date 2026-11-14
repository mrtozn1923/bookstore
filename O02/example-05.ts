function getProperty<T, K extends keyof T>(obj: T, key: K): T[K] {
  return obj[key];
}

const book = { title: "Dune", price: 450 };

const bookTitle = getProperty(book, "title"); // string
const bookPrice = getProperty(book, "price"); // number
// Error: Argument of type '"isbn"' is not assignable to parameter of type '"price" | "title"'.
getProperty(book, "isbn");
