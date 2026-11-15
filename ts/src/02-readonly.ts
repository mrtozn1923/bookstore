import { sampleCart, type CartLine } from "./cart.ts";

type ReadonlyCartLine = Readonly<CartLine>;

const couponCodes = ["AUTUMN10", "WELCOME5"] as const;
type CouponCode = (typeof couponCodes)[number];

function addLine(lines: readonly ReadonlyCartLine[], line: ReadonlyCartLine): readonly ReadonlyCartLine[] {
  return [...lines, line];
}

const cart: readonly ReadonlyCartLine[] = sampleCart();
const coupon: CouponCode = "AUTUMN10";
const withMiddlemarch = addLine(cart, { isbn: "9780141439549", title: "Middlemarch", unitPrice: 129.9, quantity: 1, isEbook: false });
console.log("lines:", cart.length, "->", withMiddlemarch.length, "coupon:", coupon);

// ES2023 copy methods
const byPrice = cart.toSorted((a, b) => a.unitPrice - b.unitPrice);
const withoutFirst = cart.toSpliced(0, 1);
const moreDune = cart.with(0, { ...cart[0], quantity: 3 });
console.log("first in cart:", cart[0].title, "| first by price:", byPrice[0].title);
console.log("withoutFirst:", withoutFirst.length, "| Dune in cart:", cart[0].quantity, "in moreDune:", moreDune[0].quantity);
