// type-challenges test helpers (the post assumes them at the top of every block)
type Equal<X, Y> =
  (<T>() => T extends X ? 1 : 2) extends (<T>() => T extends Y ? 1 : 2) ? true : false;
type Expect<T extends true> = T;

type Concat<T extends readonly unknown[], U extends readonly unknown[]> = [...T, ...U];

type Includes<T extends readonly unknown[], U> =
  T extends readonly [infer F, ...infer R]
    ? Equal<F, U> extends true
      ? true
      : Includes<R, U>
    : false;

const one = [1] as const;

type ConcatCases = [
  Expect<Equal<Concat<[], []>, []>>,
  Expect<Equal<Concat<typeof one, typeof one>, [1, 1]>>,
  Expect<Equal<Concat<["1", 2], [false, boolean]>, ["1", 2, false, boolean]>>,
  // @ts-expect-error
  Concat<null, undefined>,
];

type IncludesCases = [
  Expect<Equal<Includes<["Ada", "Alan", "Grace"], "Alan">, true>>,
  Expect<Equal<Includes<["Ada", "Alan", "Grace"], "Linus">, false>>,
  Expect<Equal<Includes<[boolean, 2, 3], false>, false>>,
  Expect<Equal<Includes<[{ a: "A" }], { readonly a: "A" }>, false>>,
  Expect<Equal<Includes<[null], undefined>, false>>,
  Expect<Equal<Includes<[1 | 2], 1>, false>>,
];
