import { distinctUntilChanged, EMPTY, exhaustMap, switchMap, takeWhile, tap, timer } from "rxjs";
import { emitAt, log, runInVirtualTime } from "../../tools/virtual-time.ts";
import { statusApi } from "./status-api.ts";

runInVirtualTime("poll only while the page is visible", () => {
  const getStatus = statusApi();
  // in the browser: fromEvent(document, "visibilitychange") mapped to document.visibilityState === "visible"
  const visible$ = emitAt([0, true], [7000, false], [19000, true]);

  visible$
    .pipe(
      distinctUntilChanged(),
      tap((visible) => log(visible ? "page visible: polling starts" : "page hidden: polling stops")),
      switchMap((visible) => (visible ? timer(0, 5000).pipe(exhaustMap(() => getStatus())) : EMPTY)),
      takeWhile((status) => status !== "Delivered", true),
    )
    .subscribe((status) => log(`screen: ${status}`));
});
