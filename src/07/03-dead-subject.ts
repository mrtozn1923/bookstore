import { BehaviorSubject, Subject } from "rxjs";

const cartEvents = new Subject<string>();
cartEvents.subscribe({ next: (e) => console.log("A next:", e), complete: () => console.log("A complete") });
cartEvents.next("Dune added");
cartEvents.complete();
cartEvents.next("Emma added"); // ignored, no error
cartEvents.subscribe({ next: (e) => console.log("B next:", e), complete: () => console.log("B complete") });

const stock = new BehaviorSubject(12);
stock.complete();
stock.subscribe({ next: (n) => console.log("C next:", n), complete: () => console.log("C complete") });
console.log("getValue() after complete:", stock.getValue());

const prices = new Subject<number>();
prices.error(new Error("price feed lost"));
prices.subscribe({ next: (p) => console.log("D next:", p), error: (err: Error) => console.log("D error:", err.message) });
