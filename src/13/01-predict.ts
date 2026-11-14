import { EMPTY, firstValueFrom, lastValueFrom, of } from "rxjs";

// the stock changes of a book that had no change today: the stream completes without a value
const todaysChanges$ = EMPTY;

// RxJS 6 style: deprecated since RxJS 7
const legacy = await todaysChanges$.toPromise();
console.log("toPromise:", legacy);

// RxJS 7 style
try {
  const last = await lastValueFrom(todaysChanges$);
  console.log("lastValueFrom:", last);
} catch (err) {
  console.log("lastValueFrom failed:", (err as Error).name, "-", (err as Error).message);
}

console.log("with defaultValue:", await lastValueFrom(todaysChanges$, { defaultValue: 0 }));
console.log("firstValueFrom(of(12, 11, 9)):", await firstValueFrom(of(12, 11, 9)));
console.log("lastValueFrom(of(12, 11, 9)):", await lastValueFrom(of(12, 11, 9)));
