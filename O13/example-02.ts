enum OrderStatus {
  Pending,
  Paid,
  Shipped,
  Delivered,
}

function fromNumber(n: number): OrderStatus {
  return n;
}

console.log(OrderStatus.Paid); // 1
console.log(OrderStatus[1]); // Paid
console.log(fromNumber(99)); // 99

// Error: Type '42' is not assignable to type 'OrderStatus'.
const invalid: OrderStatus = 42;
