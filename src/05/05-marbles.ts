import { combineLatest, concat, map, merge, race, withLatestFrom, zip } from "rxjs";
import { showMarbles } from "../../tools/marble.ts";

// two sources: letters (e.g. category changes) and digits (e.g. page changes)
const letters = "-a----b---c--|";
const digits = "---1-----2----|";
const pair = ([x, y]: [string, string]) => x + y;

showMarbles("combineLatest", (cold) => combineLatest([cold<string>(letters), cold<string>(digits)]).pipe(map(pair)));
showMarbles("withLatestFrom", (cold) => cold<string>(letters).pipe(withLatestFrom(cold<string>(digits)), map(pair)));
showMarbles("zip", (cold) => zip([cold<string>(letters), cold<string>(digits)]).pipe(map(pair)));
showMarbles("merge", (cold) => merge(cold<string>(letters), cold<string>(digits)));
showMarbles("concat", (cold) => concat(cold<string>("-a--b|"), cold<string>("-1--2|")));
showMarbles("race", (cold) => race(cold<string>("---a--b|"), cold<string>("-1--2---|")));
