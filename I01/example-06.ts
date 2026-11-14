type IsString<T> = T extends string ? "yes" : "no";

type R1 = IsString<"Dune">; // "yes": a literal type is assignable to string
type R2 = IsString<string>; // "yes"
type R3 = IsString<unknown>; // "no"
type R4 = IsString<any>; // "no" | "yes"

type HasIsbn<T> = T extends { isbn: string } ? true : false;
type R5 = HasIsbn<{ isbn: "9780441172719"; title: "Dune" }>; // true: an extra property is fine
