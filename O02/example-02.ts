function getLength<T>(value: T): number {
  // Error: Property 'length' does not exist on type 'T'.
  return value.length;
}
