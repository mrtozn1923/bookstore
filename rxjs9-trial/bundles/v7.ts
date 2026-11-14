// the same pipeline with RxJS 7.8.2 (operators from the rxjs root)
import { catchError, debounceTime, distinctUntilChanged, map, of, Subject, switchMap, timer } from "rxjs7";

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
