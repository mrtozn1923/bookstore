// One line in the cart; same field names as CartLine in Bookstore.Api
export type CartLine = {
  isbn: string;
  title: string;
  unitPrice: number;
  quantity: number;
};
