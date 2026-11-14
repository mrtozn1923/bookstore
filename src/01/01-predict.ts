import { Observable } from "rxjs";

const duneStock$ = new Observable<number>((subscriber) => {
  console.log("producer started");
  subscriber.next(12);
  subscriber.next(11);
  subscriber.complete();
  subscriber.next(10);
});

console.log("before subscribe");
duneStock$.subscribe((stock) => console.log("A got", stock));
duneStock$.subscribe((stock) => console.log("B got", stock));
console.log("after subscribe");
