import { map, take } from "rxjs";
import { showMarbles } from "../../tools/marble.ts";

// price in TRY -> price in kuruş (minor units)
showMarbles("map((price) => price * 100)", (cold) =>
  cold("-a--b--c--|", { a: 450, b: 320, c: 390 }).pipe(map((price) => price * 100)),
);

// the stock service fails after two values: # is the error
showMarbles("map((stock) => stock < 3)", (cold) =>
  cold("-a--b--#", { a: 12, b: 2 }, new Error("stock service down")).pipe(map((stock) => stock < 3)),
);

// take(2): the second value and complete land on the same frame
showMarbles("take(2)", (cold) => cold("-a--b--c--|", { a: "Dune", b: "Emma", c: "Middlemarch" }).pipe(take(2)));
