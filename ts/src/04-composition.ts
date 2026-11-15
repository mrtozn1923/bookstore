import { from, pipe as rxPipe, filter, map, reduce } from "rxjs";
import { sampleCart, type CartLine } from "./cart.ts";
import { compose, pipe } from "./fp.ts";

type PriceRule = (amount: number) => number;

// Curried rule factories: take the setting, return a rule
const percentOff = (rate: number): PriceRule => (amount) => amount * (1 - rate);
const amountOff = (discount: number): PriceRule => (amount) => Math.max(0, amount - discount);
const roundToKurus: PriceRule = (amount) => Math.round(amount * 100) / 100;

const combine = (rules: readonly PriceRule[]): PriceRule => (amount) => rules.reduce((acc, rule) => rule(acc), amount);

console.log("pipe:", pipe(249.4, percentOff(0.1), amountOff(15), roundToKurus));
console.log("member first:", pipe(249.4, amountOff(15), percentOff(0.1), roundToKurus));
console.log("compose:", compose(roundToKurus, compose(amountOff(15), percentOff(0.1)))(249.4));
console.log("combine:", combine([percentOff(0.1), amountOff(15), roundToKurus])(249.4));

// A reusable RxJS operator is a composition too
const discountedSubtotal = rxPipe(
  filter((line: CartLine) => line.quantity > 0),
  map((line) => percentOff(0.1)(line.unitPrice * line.quantity)),
  reduce((sum, amount) => sum + amount, 0),
);
from(sampleCart()).pipe(discountedSubtotal).subscribe((total) => console.log("rxjs:", roundToKurus(total)));
