const discountRate = 0.1;

function applyDiscount(price) {
  const discount = price * discountRate;
  return price - discount;
}

console.log(applyDiscount(450)); // 405
console.log(typeof discount);    // "undefined"
