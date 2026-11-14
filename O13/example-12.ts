const PaymentStatus = {
  Approved: "APPROVED",
  Declined: "DECLINED",
} as const;

type PaymentStatus = (typeof PaymentStatus)[keyof typeof PaymentStatus];

function describe(status: PaymentStatus): string {
  switch (status) {
    case PaymentStatus.Approved:
      return "ready to ship";
    case "DECLINED":
      return "payment failed";
  }
}

console.log(describe("APPROVED")); // ready to ship
console.log(describe(PaymentStatus.Declined)); // payment failed
console.log(Object.values(PaymentStatus)); // [ 'APPROVED', 'DECLINED' ]
