function getTotal(price: number, quantity: number) {
  return price * quantity;
}

// Error: Argument of type 'string' is not assignable to parameter of type 'number'.
const total = getTotal("450", 2);
console.log(total);
