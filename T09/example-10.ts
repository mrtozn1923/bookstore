type FixedDiscount = { kind: "fixed"; amount: number };
type PercentDiscount = { kind: "percent"; percent: number };
type Discount = FixedDiscount | PercentDiscount;

function applyDiscount(price: number, discount: Discount) {
  switch (discount.kind) {
    case "fixed":
      // discount: FixedDiscount
      return price - discount.amount;
    case "percent":
      // discount: PercentDiscount
      return price - (price * discount.percent) / 100;
  }
}

console.log(applyDiscount(450, { kind: "percent", percent: 10 })); // 405
