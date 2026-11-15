import { computed, signal } from "@angular/core";
import { from, filter, map, reduce } from "rxjs";
import { sampleCart, type CartLine } from "./cart.ts";

const cart = sampleCart();

// 1) Array methods
const subtotal = cart
  .filter((line) => line.quantity > 0)
  .map((line) => line.unitPrice * line.quantity)
  .reduce((sum, amount) => sum + amount, 0);
console.log("array subtotal:", subtotal.toFixed(2));

// 2) RxJS pipe
from(cart)
  .pipe(
    filter((line) => line.quantity > 0),
    map((line) => line.unitPrice * line.quantity),
    reduce((sum, amount) => sum + amount, 0),
  )
  .subscribe((total) => console.log("rxjs subtotal:", total.toFixed(2)));

// 3) Angular signals
const lines = signal<CartLine[]>(cart);
const signalSubtotal = computed(() =>
  lines()
    .filter((line) => line.quantity > 0)
    .reduce((sum, line) => sum + line.unitPrice * line.quantity, 0),
);
console.log("signal subtotal:", signalSubtotal().toFixed(2));
lines.update((current) => current.map((line) => (line.title === "Emma" ? { ...line, quantity: 2 } : line)));
console.log("after update:", signalSubtotal().toFixed(2));
