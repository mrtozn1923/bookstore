type MyReturnType<T> = T extends (...args: any[]) => infer R ? R : never;
type MyParameters<T> = T extends (...args: infer P) => any ? P : never;

function createBook(title: string, price: number) {
  return { title, price, inStock: true };
}

type NewBook = MyReturnType<typeof createBook>; // { title: string; price: number; inStock: boolean; }
type CreateBookArgs = MyParameters<typeof createBook>; // [title: string, price: number]
