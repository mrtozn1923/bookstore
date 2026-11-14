function calculateTotal(price: number, quantity: number): number {
  return price * quantity;
}

// Error: Expected 2 arguments, but got 1.
calculateTotal(450);
// Error: Expected 2 arguments, but got 3.
calculateTotal(450, 2, 5);
