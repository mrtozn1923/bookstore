type Length<T extends readonly unknown[]> = T["length"];

type A = Length<["a", "b", "c"]>;
type B = Length<string[]>;
type C = Length<"abc">;
