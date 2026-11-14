// type-challenges test helpers (the post assumes them at the top of every block)
type Equal<X, Y> =
  (<T>() => T extends X ? 1 : 2) extends (<T>() => T extends Y ? 1 : 2) ? true : false;
type Expect<T extends true> = T;

type First<T extends readonly unknown[]> = T extends readonly [infer F, ...unknown[]] ? F : never;
type Length<T extends readonly unknown[]> = T["length"];

type FirstCases = [
  Expect<Equal<First<[3, 2, 1]>, 3>>,
  Expect<Equal<First<[]>, never>>,
  Expect<Equal<First<[undefined]>, undefined>>,
  Expect<Equal<First<[() => 123, { a: string }]>, () => 123>>,
  // @ts-expect-error
  First<"notArray">,
];

type LengthCases = [
  Expect<Equal<Length<["a", "b", "c"]>, 3>>,
  Expect<Equal<Length<[]>, 0>>,
  Expect<Equal<Length<string[]>, number>>,
  // @ts-expect-error
  Length<5>,
];
