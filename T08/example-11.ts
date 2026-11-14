function formatId(id: string | number) {
  if (typeof id === "string") {
    // here id: string
    return id.toUpperCase();
  }
  // here id: number
  return id.toFixed(0);
}

console.log(formatId("order_1042")); // ORDER_1042
console.log(formatId(1042)); // 1042
