import { concatMap, exhaustMap, mergeMap, switchMap } from "rxjs";
import { showMarbles } from "../../tools/marble.ts";

// the same outer stream and the same inner Observable for all four operators:
// value a starts an inner stream that emits "A" twice, value b one that emits "B" twice
const outer = "-a--b---------|";
const inner = "--X---X|";

showMarbles("mergeMap", (cold) => cold<string>(outer).pipe(mergeMap((v) => cold(inner, { X: v.toUpperCase() }))));
showMarbles("concatMap", (cold) => cold<string>(outer).pipe(concatMap((v) => cold(inner, { X: v.toUpperCase() }))));
showMarbles("switchMap", (cold) => cold<string>(outer).pipe(switchMap((v) => cold(inner, { X: v.toUpperCase() }))));
showMarbles("exhaustMap", (cold) => cold<string>(outer).pipe(exhaustMap((v) => cold(inner, { X: v.toUpperCase() }))));
