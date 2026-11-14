interface Book {
    isbn: string;
    title: string;
    price: number;
}

const dune: Book = { isbn: "9780441172719", title: "Dune", price: "349.90" };
console.log(`Two copies of ${dune.title}: ${dune.price * 2}`);
