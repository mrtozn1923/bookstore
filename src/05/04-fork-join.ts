import { EMPTY, forkJoin, interval, map, timer } from "rxjs";
import { customers, orders } from "../../data/books.ts";
import { log, runInVirtualTime } from "../../tools/virtual-time.ts";
import { fakeRequest } from "../04/fake-api.ts";

const order = orders[0]; // order 1042: Ada, Dune + Emma

runInVirtualTime("forkJoin: three calls in parallel, one result", () => {
  forkJoin({
    order: fakeRequest(`GET /orders/${order.id}`, 300, () => order),
    customer: fakeRequest(`GET /customers/${order.customerId}`, 100, () => customers[0]),
    shipment: fakeRequest(`GET /orders/${order.id}/shipment`, 200, () => ({ status: "Shipped", trackingNo: "TRK-1042" })),
  }).subscribe(({ order, customer, shipment }) =>
    log(`order ${order.id} for ${customer.name}: ${order.items.length} items, ${order.status}, shipment ${shipment.status}`),
  );
});

runInVirtualTime("forkJoin with a source that never completes", () => {
  const liveStock$ = interval(1000).pipe(map((i) => 12 - i)); // stock updates, never completes
  forkJoin({
    order: fakeRequest(`GET /orders/${order.id}`, 300, () => order),
    stock: liveStock$,
  }).subscribe(() => log("never printed"));
  timer(5000).subscribe(() => log("still waiting after 5 seconds"));
});

runInVirtualTime("forkJoin edge cases", () => {
  forkJoin([]).subscribe({
    next: (v) => log(`[] next ${JSON.stringify(v)}`),
    complete: () => log("[] complete"),
  });
  forkJoin({ order: fakeRequest(`GET /orders/${order.id}`, 300, () => order), coupons: EMPTY }).subscribe({
    next: () => log("never printed"),
    complete: () => log("{ order, coupons: EMPTY } complete without a value"),
  });
});
