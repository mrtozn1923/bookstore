import { Observable } from "rxjs";

const orderStatus$ = new Observable<string>((subscriber) => {
  subscriber.next("Pending");
  subscriber.next("Paid");
  subscriber.error(new Error("payment provider timeout"));
  subscriber.next("Shipped");
  subscriber.complete();
  console.log("producer: closed =", subscriber.closed);
  return () => console.log("teardown");
});

orderStatus$.subscribe({
  next: (status) => console.log("next:", status),
  error: (err: Error) => console.log("error:", err.message),
  complete: () => console.log("complete"),
});
