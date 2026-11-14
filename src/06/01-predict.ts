import { concat, debounceTime, NEVER } from "rxjs";
import { emitAt, log, runInVirtualTime } from "../../tools/virtual-time.ts";

runInVirtualTime("review draft autosave", () => {
  // Ada writes a review of Dune: three quick edits, a pause, two more edits
  const draft$ = concat(
    emitAt(
      [0, "Great"],
      [200, "Great book"],
      [400, "Great book, slow start"],
      [1500, "Great book, slow start but"],
      [1700, "Great book, slow start but worth it"],
    ),
    NEVER, // the text box stays open: the stream never completes
  );

  draft$.pipe(debounceTime(500)).subscribe((text) => log(`save: "${text}"`));
});
