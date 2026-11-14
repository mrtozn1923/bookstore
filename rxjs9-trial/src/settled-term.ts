import "rxjs";
import { debounce } from "rxjs/debounce";
import { distinctUntilChanged } from "rxjs/distinct-until-changed";
import { map } from "rxjs/map";

// RxJS 9 has no OperatorFunction: a custom operator is an ordinary function
export function settledTerm(terms: Observable<string>, dueTimeMs = 300): Observable<string> {
  return terms[debounce](dueTimeMs)[map]((term) => term.trim())[distinctUntilChanged]();
}
