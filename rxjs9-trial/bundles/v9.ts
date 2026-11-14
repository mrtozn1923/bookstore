// the same pipeline with RxJS 9.0.0-beta.1 (Symbol operators)
import { Subject } from "rxjs";
import { catchError } from "rxjs/catch-error";
import { debounce } from "rxjs/debounce";
import { distinctUntilChanged } from "rxjs/distinct-until-changed";
import { map } from "rxjs/map";
import { switchMap } from "rxjs/switch-map";
import { timer } from "rxjs/timer";

const terms = new Subject<string>();
terms[debounce](300)
  [map]((term) => term.trim())
  [distinctUntilChanged]()
  [switchMap]((term) => Observable[timer](500)[map](() => [term]))
  [catchError](() => [[] as string[]])
  .subscribe((titles) => console.log(titles));
terms.next("dune");
