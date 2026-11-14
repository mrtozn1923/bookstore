function welcome(name: string, greeting?: string) {
  // greeting: string | undefined
  return `${greeting ?? "Welcome back"}, ${name}!`;
}

function lineTotal(price: number, quantity = 1) {
  // quantity: number
  return price * quantity;
}

function cartTotal(...prices: number[]) {
  return prices.reduce((total, price) => total + price, 0);
}

console.log(welcome("Ada")); // Welcome back, Ada!
console.log(welcome("Ada", "Hello")); // Hello, Ada!
console.log(lineTotal(450)); // 450
console.log(cartTotal(450, 320)); // 770
