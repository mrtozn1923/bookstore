type Equal<X, Y> =
  (<T>() => T extends X ? 1 : 2) extends (<T>() => T extends Y ? 1 : 2) ? true : false;
type Expect<T extends true> = T;

type MyExclude<T, U> = T extends U ? never : T;

type Wrong = Expect<Equal<MyExclude<"a" | "b" | "c", "a">, "b">>; // Error
