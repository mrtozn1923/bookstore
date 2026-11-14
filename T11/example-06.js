function getDiscount(total) {
  if (total < 100) {
    return 0;
  }
  if (total < 500) {
    return total * 0.05;
  }
  return total * 0.1;
}

console.log(getDiscount(90));   // 0
console.log(getDiscount(320));  // 16
console.log(getDiscount(1160)); // 116
