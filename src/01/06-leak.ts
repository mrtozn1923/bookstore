import { Observable } from "rxjs";

let ticks = 0;

const leakyStock$ = new Observable<number>((subscriber) => {
  setInterval(() => {
    ticks++;
    subscriber.next(ticks);
  }, 100);
  // no teardown returned: nobody can stop this interval
});

const sub = leakyStock$.subscribe((n) => console.log("value", n));
setTimeout(() => {
  sub.unsubscribe();
  console.log("unsubscribed at tick", ticks);
}, 250);
setTimeout(() => {
  console.log("ticks one second later:", ticks);
  process.exit(0);
}, 1250);
