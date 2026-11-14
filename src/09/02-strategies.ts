import { catchError, EMPTY, of, throwError } from "rxjs";
import { log, runInVirtualTime } from "../../tools/virtual-time.ts";
import { fakeHttp } from "./fake-http.ts";

const stock = () => fakeHttp("GET /books/9780441172719/stock", 200, 503, () => 12);
const report = (label: string) => ({
  next: (v: unknown) => log(`${label} next: ${v}`),
  error: (err: Error) => log(`${label} error: ${err.message}${err.cause ? ` (cause: ${(err.cause as Error).message})` : ""}`),
  complete: () => log(`${label} complete`),
});

runInVirtualTime("1. replace: show the last known value", () => {
  stock().pipe(catchError(() => of("12 (cached)"))).subscribe(report("replace"));
});

runInVirtualTime("2. rethrow: add context, let the caller decide", () => {
  stock()
    .pipe(catchError((err: Error) => throwError(() => new Error("Could not load Dune's stock", { cause: err }))))
    .subscribe(report("rethrow"));
});

runInVirtualTime("3. EMPTY: give up silently", () => {
  stock().pipe(catchError(() => EMPTY)).subscribe(report("empty"));
});
