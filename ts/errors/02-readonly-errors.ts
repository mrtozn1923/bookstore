import { sampleCart, type CartLine } from "../src/cart.ts";

const cart: readonly Readonly<CartLine>[] = sampleCart();
cart.push(cart[0]);
cart[0].quantity = 3;
const coupon: "AUTUMN10" | "WELCOME5" = "SPRING20";
