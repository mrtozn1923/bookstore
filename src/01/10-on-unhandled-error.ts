import { config, Observable } from "rxjs";

// one central place for errors that no subscriber handles
config.onUnhandledError = (err) => {
  console.log("[log] unhandled RxJS error:", (err as Error).message);
};

const price$ = new Observable<number>((subscriber) => {
  subscriber.error(new Error("price service returned 503"));
});

price$.subscribe((price) => console.log("price", price)); // no error callback
price$.subscribe({
  next: (price) => console.log("price", price),
  error: (err: Error) => console.log("handled here:", err.message),
});
console.log("after subscribe");
