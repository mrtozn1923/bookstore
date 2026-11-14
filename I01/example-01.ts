type IsString<T> = T extends string ? "yes" : "no";

type A = IsString<"Dune">;
type B = IsString<string | number>;
type C = IsString<never>;
