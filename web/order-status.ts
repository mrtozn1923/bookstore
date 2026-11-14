import { distinctUntilChanged, EMPTY, exhaustMap, fromEvent, map, startWith, switchMap, takeWhile, timer } from "rxjs";
import { fromFetch } from "rxjs/fetch";

type OrderStatus = { id: number; status: "Paid" | "Shipped" | "Delivered" };

const statusText = document.querySelector<HTMLParagraphElement>("#status")!;

const getStatus = () =>
  fromFetch("http://localhost:5180/api/orders/1042/status", {
    selector: (response) => response.json() as Promise<OrderStatus>,
  });

const visible$ = fromEvent(document, "visibilitychange").pipe(
  map(() => document.visibilityState === "visible"),
  startWith(document.visibilityState === "visible"),
  distinctUntilChanged(),
);

visible$
  .pipe(
    switchMap((visible) => (visible ? timer(0, 5000).pipe(exhaustMap(() => getStatus())) : EMPTY)),
    takeWhile((order) => order.status !== "Delivered", true),
  )
  .subscribe((order) => {
    statusText.textContent = `Order ${order.id}: ${order.status}`;
    console.log(statusText.textContent);
  });
