import { asapScheduler, asyncScheduler, observeOn, of, queueScheduler, subscribeOn } from "rxjs";

// real time, no TestScheduler: in which order does each line print?
console.log("1. script start");
setTimeout(() => console.log("setTimeout(0)"), 0);
Promise.resolve().then(() => console.log("Promise.then"));

of("asyncScheduler").pipe(observeOn(asyncScheduler)).subscribe((v) => console.log(`observeOn ${v}`));
of("asapScheduler").pipe(observeOn(asapScheduler)).subscribe((v) => console.log(`observeOn ${v}`));
of("queueScheduler").pipe(observeOn(queueScheduler)).subscribe((v) => console.log(`observeOn ${v}`));
of("no scheduler").subscribe((v) => console.log(v));
of("subscribeOn asyncScheduler").pipe(subscribeOn(asyncScheduler)).subscribe((v) => console.log(v));

console.log("2. script end");
