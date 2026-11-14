import { EMPTY, first, last, of } from "rxjs";
import { showMarbles } from "../../tools/marble.ts";

const prices = { a: 450, b: 320, c: 390 };

showMarbles("first()", (cold) => cold("-a--b--c--|", prices).pipe(first()));
showMarbles("first((p) => p < 400)", (cold) => cold("-a--b--c--|", prices).pipe(first((p) => p < 400)));
showMarbles("last()", (cold) => cold("-a--b--c--|", prices).pipe(last()));
showMarbles("first() on an empty stream", (cold) => cold("----|").pipe(first()));

// no book under 100 TRY: the error message, and the default value that avoids it
of(450, 320, 390).pipe(first((p) => p < 100)).subscribe({
  error: (err: Error) => console.log(`${err.name}: ${err.message}`),
});
EMPTY.pipe(first(undefined, "no books")).subscribe((v) => console.log("with default:", v));
