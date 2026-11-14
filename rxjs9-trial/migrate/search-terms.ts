// RxJS 7 input for the @rxjs/migrate dry run in the post (not compiled in this project)
import { debounceTime, filter, map, type Observable } from "rxjs";

// the search box: wait for a pause, trim, ignore empty terms
export function searchTerms(input: Observable<string>): Observable<string> {
  return input.pipe(
    debounceTime(300),
    map((term) => term.trim()),
    filter((term) => term.length > 0),
  );
}
