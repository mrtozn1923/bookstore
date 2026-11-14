type FirstString<T> = T extends [infer F extends string, ...unknown[]] ? F : never;
type FS1 = FirstString<["Dune", 450]>; // "Dune"
type FS2 = FirstString<[450, "Dune"]>; // never

type ToNumber<T> = T extends `${infer N extends number}` ? N : never;
type T1 = ToNumber<"42">; // 42
type T2 = ToNumber<"1.0">; // number
type T3 = ToNumber<"abc">; // never
