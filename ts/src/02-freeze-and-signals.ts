import { computed, signal } from "@angular/core";
import { sampleCart, type CartLine } from "./cart.ts";

// Object.freeze is shallow
const frozen = Object.freeze(sampleCart());
try {
  (frozen as CartLine[]).push(frozen[0]);
} catch (error) {
  console.log("push:", (error as Error).constructor.name);
}
frozen[0].quantity = 5;
console.log("nested change kept:", frozen[0].quantity);

// Signals compare references with Object.is
const lines = signal<CartLine[]>(sampleCart());
const count = computed(() => lines().filter((line) => line.quantity > 0).length);
console.log("count:", count());

lines().push({ isbn: "9780141439549", title: "Middlemarch", unitPrice: 129.9, quantity: 1, isEbook: false });
lines.set(lines());
console.log("after push + set(same array):", count(), "| actual:", lines().filter((line) => line.quantity > 0).length);

lines.update((current) => [...current]);
console.log("after new reference:", count());
