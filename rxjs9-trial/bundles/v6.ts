// the search box pipeline with RxJS 6.6.7 (operators from rxjs/operators)
import { of, Subject, timer } from "rxjs6";
import { catchError, debounceTime, distinctUntilChanged, map, switchMap } from "rxjs6/operators";

const terms = new Subject<string>();
terms
  .pipe(
    debounceTime(300),
    map((term) => term.trim()),
    distinctUntilChanged(),
    switchMap((term) => timer(500).pipe(map(() => [term]))),
    catchError(() => of([] as string[])),
  )
  .subscribe((titles) => console.log(titles));
terms.next("dune");
