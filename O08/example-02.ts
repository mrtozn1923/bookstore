type Discount =
  | { kind: "percent"; percent: number }
  | { kind: "fixed"; amount: number }
  | { kind: "bundle"; buy: number; free: number };

type PercentDiscount = Extract<Discount, { kind: "percent" }>;
// { kind: "percent"; percent: number; }

type OtherDiscount = Exclude<Discount, { kind: "percent" }>;
// { kind: "fixed"; amount: number; } | { kind: "bundle"; buy: number; free: number; }

function applyPercent(discount: PercentDiscount, price: number): number {
  return price * (1 - discount.percent / 100);
}

console.log(applyPercent({ kind: "percent", percent: 10 }, 450).toFixed(2)); // 405.00
