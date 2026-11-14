// type-challenges test helpers (the post assumes them at the top of every block)
type Equal<X, Y> =
  (<T>() => T extends X ? 1 : 2) extends (<T>() => T extends Y ? 1 : 2) ? true : false;
type Expect<T extends true> = T;

type TupleToObject<T extends readonly PropertyKey[]> = { [V in T[number]]: V };

const formats = ["hardcover", "paperback", "ebook"] as const;

type TupleCases = [
  Expect<Equal<TupleToObject<typeof formats>, { hardcover: "hardcover"; paperback: "paperback"; ebook: "ebook" }>>,
  Expect<Equal<TupleToObject<readonly [1, 2]>, { 1: 1; 2: 2 }>>,
  // @ts-expect-error
  TupleToObject<[[1, 2], {}]>,
];
