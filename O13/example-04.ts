enum PaymentStatus {
  Approved = "APPROVED",
  Declined = "DECLINED",
}

function canShip(status: PaymentStatus): boolean {
  return status === PaymentStatus.Approved;
}

console.log(canShip(PaymentStatus.Approved)); // true

// Error: Argument of type '"APPROVED"' is not assignable to parameter of type 'PaymentStatus'.
canShip("APPROVED");
