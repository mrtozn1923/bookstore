import { EMPTY, NEVER, throwError, type Observable } from "rxjs";

function watch(label: string, source$: Observable<unknown>) {
  source$.subscribe({
    next: (v) => console.log(label, "next", v),
    error: (err: Error) => console.log(label, "error:", err.message),
    complete: () => console.log(label, "complete"),
  });
}

watch("EMPTY:", EMPTY);
watch("NEVER:", NEVER); // prints nothing, ever
watch("throwError:", throwError(() => new Error("Order 1042 not found")));
console.log("done");
