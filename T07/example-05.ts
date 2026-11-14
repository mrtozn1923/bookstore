interface Book {
  readonly isbn: string;
  title: string;
  price: number;
  discount?: number;
  describe(): string;
}

const dune: Book = {
  isbn: "9780441172719",
  title: "Dune",
  price: 450,
  describe() {
    return `${this.title}: ${this.price} TRY`;
  },
};
