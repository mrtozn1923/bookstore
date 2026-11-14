function getDiscount(quantity: number): number {
  if (quantity >= 20) {
    return 0.1;
  }
  // Error: Type 'string' is not assignable to type 'number'.
  return "none";
}
