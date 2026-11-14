// type-challenges test helpers (the post assumes them at the top of every block)
type Equal<X, Y> =
  (<T>() => T extends X ? 1 : 2) extends (<T>() => T extends Y ? 1 : 2) ? true : false;
type Expect<T extends true> = T;

type MyAwaited<T extends PromiseLike<any>> =
  T extends PromiseLike<infer V> ? (V extends PromiseLike<any> ? MyAwaited<V> : V) : never;

type Thenable = { then: (onfulfilled: (arg: number) => any) => any };

type AwaitedCases = [
  Expect<Equal<MyAwaited<Promise<string>>, string>>,
  Expect<Equal<MyAwaited<Promise<Promise<number | boolean>>>, number | boolean>>,
  Expect<Equal<MyAwaited<Thenable>, number>>,
  // @ts-expect-error
  MyAwaited<number>,
];
