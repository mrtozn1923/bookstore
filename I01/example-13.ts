function describe<T extends string | number>(value: T): T extends string ? "text" : "number" {
  // Error: Type '"text"' is not assignable to type 'T extends string ? "text" : "number"'.
  return typeof value === "string" ? "text" : "number";
}
