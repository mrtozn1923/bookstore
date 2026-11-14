function fail(message: string): never {
  throw new Error(message);
}

// Error: Type '1' is not assignable to type 'never'.
let impossible: never = 1;
