// Node 24 has no native Observable: importing rxjs installs @rxjs/observable-polyfill
import "./before-rxjs.ts";
import { ColdObservable } from "rxjs";
import { map } from "rxjs/map";
import { scan } from "rxjs/scan";

console.log("Observable after rxjs:", typeof Observable);

type CartItem = { title: string; price: number; quantity: number };

// the cart: 2 x Dune, 1 x Emma, 1 x The Hobbit
const cart = new ColdObservable<CartItem>((subscriber) => {
  subscriber.next({ title: "Dune", price: 450, quantity: 2 });
  subscriber.next({ title: "Emma", price: 320, quantity: 1 });
  subscriber.next({ title: "The Hobbit", price: 390, quantity: 1 });
  subscriber.complete();
});

// operators are Symbols: source[map](…) instead of source.pipe(map(…))
const result = cart[map]((item) => item.price * item.quantity)
  [scan]((total, line) => total + line, 0)
  .subscribe({
    next: (total) => console.log(`subtotal: ${total} TRY`),
    complete: () => console.log("done"),
  });

console.log("subscribe() returns:", result);
console.log("cart.pipe:", (cart as unknown as { pipe?: unknown }).pipe);
