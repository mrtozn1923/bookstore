const book: { readonly isbn: string; title: string; tags: string[] } = {
  isbn: "9780441172719",
  title: "Dune",
  tags: [],
};

book.title = "Dune (Deluxe Edition)";
// Error: Cannot assign to 'isbn' because it is a read-only property.
book.isbn = "9780441013593";
book.tags.push("sale");
