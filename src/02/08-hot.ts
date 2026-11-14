import { fromEvent, map } from "rxjs";

// hot: the warehouse fires stock events whether anyone listens or not
const warehouse = new EventTarget();
const restocked$ = fromEvent(warehouse, "restocked").pipe(
  map((event) => (event as CustomEvent<string>).detail),
);

["Dune", "Emma", "The Hobbit", "Middlemarch"].forEach((title, i) => {
  setTimeout(() => {
    console.log(`warehouse: ${title} restocked`);
    warehouse.dispatchEvent(new CustomEvent("restocked", { detail: title }));
  }, 100 * (i + 1));
});

restocked$.subscribe((title) => console.log("  hot A:", title));
setTimeout(() => restocked$.subscribe((title) => console.log("  hot B:", title)), 250);
