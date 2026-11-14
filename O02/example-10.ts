function withDefaultId<T extends { id: number }>(item: T): T {
  // Error: Type '{ id: number; }' is not assignable to type 'T'.
  return { id: 1 };
}
