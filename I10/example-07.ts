// type-challenges test helpers (the post assumes them at the top of every block)
type Equal<X, Y> =
  (<T>() => T extends X ? 1 : 2) extends (<T>() => T extends Y ? 1 : 2) ? true : false;
type Expect<T extends true> = T;

type MyExclude<T, U> = T extends U ? never : T;
type If<C extends boolean, T, F> = C extends true ? T : F;

type ExcludeCases = [
  Expect<Equal<MyExclude<"a" | "b" | "c", "a">, "b" | "c">>,
  Expect<Equal<MyExclude<string | number | (() => void), Function>, string | number>>,
];

type IfCases = [
  Expect<Equal<If<true, "a", "b">, "a">>,
  Expect<Equal<If<false, "a", 2>, 2>>,
  Expect<Equal<If<boolean, "a", 2>, "a" | 2>>,
  // @ts-expect-error
  If<null, "a", "b">,
];
