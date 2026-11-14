class CartLine {
  readonly isbn: string;
  private quantity: number;

  constructor(isbn: string, quantity: number) {
    this.isbn = isbn;
    this.quantity = quantity;
  }
}
