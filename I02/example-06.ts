type First<T> = T extends [infer Head, ...unknown[]] ? Head : never;
type Last<T> = T extends [...unknown[], infer Tail] ? Tail : never;
type Rest<T> = T extends [unknown, ...infer R] ? R : never;

type F = First<[string, number, boolean]>; // string
type L = Last<[string, number, boolean]>; // boolean
type R = Rest<[string, number, boolean]>; // [number, boolean]

type E1 = First<[]>; // never
type E2 = First<string[]>; // never
