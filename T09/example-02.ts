function formatPrice(price: string | number) {
  // price: string | number
  if (typeof price === "string") {
    price = Number.parseFloat(price);
    // price: number
  }
  // price: number
  return price.toFixed(2);
}

console.log(formatPrice("450")); // 450.00
console.log(formatPrice(389.9)); // 389.90
