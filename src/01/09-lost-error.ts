import { Observable } from "rxjs";

const cart$ = new Observable<{ title: string; price: number | null }>((subscriber) => {
  try {
    subscriber.next({ title: "Dune", price: 450 });
    subscriber.next({ title: "Emma", price: null }); // price missing in the API response
    subscriber.next({ title: "The Hobbit", price: 390 });
    subscriber.complete();
  } catch (err) {
    console.log("producer caught:", err); // never runs
    subscriber.error(err);
  }
});

console.log("before subscribe");
cart$.subscribe({
  next: (item) => console.log(item.title, item.price!.toFixed(2)),
  error: (err) => console.log("error callback:", err), // never runs
  complete: () => console.log("complete"),
});
console.log("after subscribe");
