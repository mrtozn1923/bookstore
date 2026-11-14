type Book = {
  id: string;
  title: string;
  price: number;
  stock: number;
  createdAt: Date;
};

type NewBook = Omit<Book, "id" | "createdAt">;
type BookPatch = Partial<NewBook>;
type BookErrors = Partial<Record<keyof NewBook, string>>;

function validate(input: NewBook): BookErrors {
  const errors: BookErrors = {};
  if (input.title.trim() === "") errors.title = "Title is required";
  if (input.price <= 0) errors.price = "Price must be positive";
  return errors;
}

const patch: BookPatch = { stock: 0 };
console.log(validate({ title: "", price: 0, stock: 5 }));
// { title: 'Title is required', price: 'Price must be positive' }
