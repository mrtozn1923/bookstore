function getDiscount(quantity: number) {
  if (quantity >= 20) {
    return 0.1;
  }
  return "none";
}

// Error: The right-hand side of an arithmetic operation must be of type 'any', 'number', 'bigint' or an enum type.
const finalPrice = 450 * 20 * (1 - getDiscount(20));
