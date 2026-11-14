import { concat, finalize, NEVER, switchMap } from "rxjs";
import { emitAt, log, runInVirtualTime } from "../../tools/virtual-time.ts";
import { fakeHttp } from "./fake-http.ts";

let loading = false;

function load(label: string, status: number) {
  loading = true;
  log(`${label}: loading = true`);
  return fakeHttp(`GET /books (${label})`, 300, status, () => 6).pipe(
    // runs on complete, on error and on unsubscribe
    finalize(() => {
      loading = false;
      log(`${label}: finalize, loading = false`);
    }),
  );
}

runInVirtualTime("success, error, cancel", () => {
  load("success", 200).subscribe({ next: (n) => log(`success: ${n} books`), complete: () => log("success: complete") });
  emitAt([1000, "x"]).subscribe(() => load("error", 503).subscribe({ error: (err: Error) => log(`error: ${err.message}`) }));
  // a newer request cancels this one through switchMap
  concat(emitAt([2000, "first"], [2100, "second"]), NEVER)
    .pipe(switchMap((label) => load(label, 200)))
    .subscribe((n) => log(`switchMap: ${n} books, loading = ${loading}`));
});
