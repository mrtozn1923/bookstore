// RxJS 9 on top of the browser's Observable: EventTarget.when() + RxJS Symbol operators
import { getObservablePolyfillInfo } from "@rxjs/observable-polyfill";
import "rxjs";
import { debounce } from "rxjs/debounce";
import { distinctUntilChanged } from "rxjs/distinct-until-changed";
import { map } from "rxjs/map";

console.log(`Observable: ${getObservablePolyfillInfo() ? "RxJS fallback" : "native"}`);

const box = document.querySelector<HTMLInputElement>("#search")!;
box
  .when("input")
  .map(() => box.value) // the platform's own map
  [debounce](300) // RxJS operators via Symbols
  [map]((term) => term.trim())
  [distinctUntilChanged]()
  .subscribe((term) => console.log(`search: ${term}`));
