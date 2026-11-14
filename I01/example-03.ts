type ElementType<T> = T extends readonly unknown[] ? T[number] : T;

type E1 = ElementType<string[]>; // string
type E2 = ElementType<readonly [1, 2]>; // 1 | 2
type E3 = ElementType<boolean>; // boolean
