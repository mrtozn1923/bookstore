type FixedDiscount = { kind: "fixed"; amount: number };
type PercentDiscount = { kind: "percent"; percent: number };
type BundleDiscount = { kind: "bundle"; minQuantity: number; percent: number };
type Discount = FixedDiscount | PercentDiscount | BundleDiscount;

function applyDiscount(price: number, discount: Discount): number {
  switch (discount.kind) {
    case "fixed":
      return price - discount.amount;
    case "percent":
      return price - (price * discount.percent) / 100;
    default: {
      // Error: Type 'BundleDiscount' is not assignable to type 'never'.
      const unhandled: never = discount;
      return unhandled;
    }
  }
}
