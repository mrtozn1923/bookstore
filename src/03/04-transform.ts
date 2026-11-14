import { pairwise, reduce, scan } from "rxjs";
import { showMarbles } from "../../tools/marble.ts";

// cart changes in TRY: +Dune, +Emma, -Dune, +The Hobbit
const cartChanges = { a: 450, b: 320, c: -450, d: 390 };

showMarbles("scan((total, change) => total + change, 0)", (cold) =>
  cold("-a--b--c--d--|", cartChanges).pipe(scan((total, change) => total + change, 0)),
);

showMarbles("reduce((total, change) => total + change, 0)", (cold) =>
  cold("-a--b--c--d--|", cartChanges).pipe(reduce((total, change) => total + change, 0)),
);

// Dune's stock level; pairwise gives [previous, current]
showMarbles("pairwise()", (cold) => cold("-a--b--c--|", { a: 12, b: 11, c: 9 }).pipe(pairwise()));
