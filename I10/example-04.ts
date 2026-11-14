// type-challenges test helpers (the post assumes them at the top of every block)
type Equal<X, Y> =
  (<T>() => T extends X ? 1 : 2) extends (<T>() => T extends Y ? 1 : 2) ? true : false;
type Expect<T extends true> = T;

type MyPick<T, K extends keyof T> = { [P in K]: T[P] };
type MyReadonly<T> = { readonly [P in keyof T]: T[P] };

interface Book {
  title: string;
  author: string;
  inStock: boolean;
}

type PickCases = [
  Expect<Equal<MyPick<Book, "title">, { title: string }>>,
  Expect<Equal<MyPick<Book, "title" | "inStock">, { title: string; inStock: boolean }>>,
  // @ts-expect-error
  MyPick<Book, "title" | "invalid">,
];

type ReadonlyCases = [Expect<Equal<MyReadonly<Book>, Readonly<Book>>>];
