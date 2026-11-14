import { defer, exhaustMap, mergeMap, switchMap, type OperatorFunction } from "rxjs";
import { emitAt, log, runInVirtualTime } from "../../tools/virtual-time.ts";
import { fakeRequest } from "./fake-api.ts";

let nextOrderId = 1042;

// the server creates the order as soon as the request arrives, then answers after 500 ms
function placeOrder() {
  return defer(() => {
    const orderId = nextOrderId++;
    return fakeRequest(`POST /orders (order ${orderId})`, 500, () => orderId, () =>
      log(`   server: order ${orderId} created for Ada, 770 TRY charged`),
    );
  });
}

function runCheckout(name: string, flatten: OperatorFunction<string, number>) {
  nextOrderId = 1042;
  runInVirtualTime(name, () => {
    // Ada double-clicks "Place Order", then clicks once more
    const clicks$ = emitAt([0, "click"], [80, "click"], [160, "click"]);
    clicks$.pipe(flatten).subscribe((id) => log(`screen: thank you, order ${id}`));
  });
}

runCheckout("mergeMap", mergeMap(() => placeOrder()));
runCheckout("switchMap", switchMap(() => placeOrder()));
runCheckout("exhaustMap", exhaustMap(() => placeOrder()));
