import { retry, timeout, timer } from "rxjs";
import { log, runInVirtualTime } from "../../tools/virtual-time.ts";
import { fakeRequest } from "../04/fake-api.ts";

// the server creates the order as soon as the request arrives, but answers slowly (1500 ms)
function bookstoreServer() {
  const orders = new Map<string, number>(); // idempotency key -> order id
  let nextId = 1043;
  return {
    placeOrder(idempotencyKey?: string) {
      return fakeRequest(`POST /orders${idempotencyKey ? ` (Idempotency-Key: ${idempotencyKey})` : ""}`, 1500, () => "201", () => {
        if (idempotencyKey && orders.has(idempotencyKey)) {
          log(`   server: key seen before, returns order ${orders.get(idempotencyKey)}`);
          return;
        }
        const id = nextId++;
        if (idempotencyKey) orders.set(idempotencyKey, id);
        log(`   server: order ${id} created`);
      });
    },
  };
}

runInVirtualTime("retry on a POST: every attempt is a new order", () => {
  const server = bookstoreServer();
  server
    .placeOrder()
    .pipe(timeout(1000), retry({ count: 2, delay: () => timer(500) }))
    .subscribe({ next: (r) => log(`client: ${r}`), error: (err: Error) => log(`client: ${err.name}`) });
});

runInVirtualTime("the same retry with an idempotency key", () => {
  const server = bookstoreServer();
  const key = "c1f0e6a2"; // created once per click, sent with every attempt
  server
    .placeOrder(key)
    .pipe(timeout(1000), retry({ count: 2, delay: () => timer(500) }))
    .subscribe({ next: (r) => log(`client: ${r}`), error: (err: Error) => log(`client: ${err.name}`) });
});
