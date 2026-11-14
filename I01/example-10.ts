type OnlyStrings<T> = T extends string ? T : never;

type S1 = OnlyStrings<"Dune" | 450 | "Emma" | true>; // "Dune" | "Emma"
