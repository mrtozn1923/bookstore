class Book {
  constructor(title, price) {
    this.title = title;
    this.price = price; // the setter runs here too
  }

  get price() {
    return this._price;
  }

  set price(value) {
    if (typeof value !== "number" || value < 0) {
      throw new RangeError("Price cannot be negative");
    }
    this._price = value;
  }
}

const dune = new Book("Dune", 450);
dune.price = -5; // RangeError: Price cannot be negative
