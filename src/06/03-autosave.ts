import { concat, concatMap, debounceTime, distinctUntilChanged, NEVER, takeUntil } from "rxjs";
import { emitAt, log, runInVirtualTime } from "../../tools/virtual-time.ts";
import { fakeRequest } from "../04/fake-api.ts";

function saveDraft(text: string) {
  return fakeRequest(`PUT /reviews/dune/draft (${text.length} chars)`, 300, () => text);
}

// what Ada types into the review box (the box stays open, so the stream never completes)
function typing() {
  return concat(
    emitAt(
      [0, "Great book"],
      [300, "Great book, slow start"],
      [1400, "Great book, slow start"], // a typo typed and deleted again: same text
      [2600, "Great book, slow start but worth it"],
    ),
    NEVER,
  );
}

const report = { next: (text: string) => log(`saved: "${text}"`), complete: () => log("autosave stopped") };

runInVirtualTime("takeUntil after debounceTime", () => {
  const leave$ = emitAt([2900, "leave"]); // Ada clicks "Back to catalog" 300 ms after her last edit
  typing()
    .pipe(
      debounceTime(800),
      distinctUntilChanged(),
      concatMap((text) => saveDraft(text)),
      takeUntil(leave$), // leaving drops the edit still waiting in debounceTime
    )
    .subscribe(report);
});

runInVirtualTime("takeUntil before debounceTime", () => {
  const leave$ = emitAt([2900, "leave"]);
  typing()
    .pipe(
      takeUntil(leave$), // the source completes, so debounceTime emits the waiting edit at once
      debounceTime(800),
      distinctUntilChanged(),
      concatMap((text) => saveDraft(text)),
    )
    .subscribe(report);
});
