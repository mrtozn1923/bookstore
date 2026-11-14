import { interval, take } from "rxjs";

// cold: every subscriber gets its own interval, starting from 0
const cold$ = interval(100).pipe(take(4));

cold$.subscribe((v) => console.log("cold A:", v));
setTimeout(() => cold$.subscribe((v) => console.log("cold B:", v)), 250);
