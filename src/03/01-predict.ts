import { distinctUntilChanged } from "rxjs";
import { showMarbles } from "../../tools/marble.ts";

// the catalog page emits the current filter every time the user clicks "Apply"
showMarbles("distinctUntilChanged()", (cold) =>
  cold("-a--b--c--|", {
    a: { category: "classics" },
    b: { category: "classics" },
    c: { category: "fantasy" },
  }).pipe(distinctUntilChanged()),
);
