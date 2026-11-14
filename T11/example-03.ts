type BookSummary = { title: string };
type Book = { title: string; price: number };

const dune: Book = { title: "Dune", price: 450 };
const summary: BookSummary = dune; // compiles: Book has everything BookSummary asks for

// Error: Property 'price' is missing in type 'BookSummary' but required in type 'Book'.
const back: Book = summary;
