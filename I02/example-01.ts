type ElementOf<T> = T extends (infer Item)[] ? Item : never;

type A = ElementOf<string[]>;
type B = ElementOf<[number, boolean]>;
type C = ElementOf<readonly string[]>;
