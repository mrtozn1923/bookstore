type Book = { title: string; price: number };

const first = { title: "Dune" } as Book;
const second: Book = { title: "Dune" };
const third = "Dune" as Book;
