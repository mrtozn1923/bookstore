import { concatMap, mergeMap, type OperatorFunction } from "rxjs";
import { emitAt, log, runInVirtualTime } from "../../tools/virtual-time.ts";
import { fakeRequest } from "./fake-api.ts";

// network jitter: in this run the later requests happen to be answered faster
const latency: Record<number, number> = { 2: 300, 3: 200, 4: 100 };

function setQuantity(quantity: number) {
  return fakeRequest(`PUT /cart/items/9780441172719 quantity=${quantity}`, latency[quantity], () => quantity);
}

function runStepper(name: string, flatten: OperatorFunction<number, number>) {
  runInVirtualTime(name, () => {
    // the user clicks "+" on Dune three times: 1 -> 2 -> 3 -> 4
    const quantities$ = emitAt([0, 2], [50, 3], [100, 4]);
    quantities$.pipe(flatten).subscribe((q) => log(`cart line shows quantity ${q}`));
  });
}

runStepper("mergeMap", mergeMap((q) => setQuantity(q)));
runStepper("concatMap", concatMap((q) => setQuantity(q)));
