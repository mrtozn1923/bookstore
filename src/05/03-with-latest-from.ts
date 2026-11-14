import { combineLatest, map, startWith, withLatestFrom } from "rxjs";
import { emitAt, log, runInVirtualTime } from "../../tools/virtual-time.ts";

runInVirtualTime("withLatestFrom: the click decides, the form only supplies context", () => {
  // the institutional order form: the buyer edits fields, then presses "Request Quote"
  const quantity$ = emitAt([100, 20], [200, 25], [600, 30]).pipe(startWith(10));
  const institution$ = emitAt([150, "Hillside School"]);
  const quoteClicks$ = emitAt([50, "click"], [400, "click"], [800, "click"]);

  quoteClicks$
    .pipe(
      withLatestFrom(quantity$, institution$),
      map(([, quantity, institution]) => `quote: ${quantity} x Dune for ${institution}`),
    )
    .subscribe((line) => log(line));
});

runInVirtualTime("combineLatest: every field change fires", () => {
  const quantity$ = emitAt([100, 20], [200, 25], [600, 30]).pipe(startWith(10));
  const institution$ = emitAt([150, "Hillside School"]);

  combineLatest([quantity$, institution$])
    .pipe(map(([quantity, institution]) => `quote: ${quantity} x Dune for ${institution}`))
    .subscribe((line) => log(line));
});
