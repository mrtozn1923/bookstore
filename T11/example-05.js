function calculateTotal(price, quantity) {
  return price * quantity;
}

const total = calculateTotal(450, 2);
console.log(total);                       // 900
console.log(calculateTotal(320, 1) + 40); // 360
