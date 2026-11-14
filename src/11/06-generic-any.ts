import { catchError, map, of, startWith, type OperatorFunction } from "rxjs";
import { books } from "../../data/books.ts";
import { loadingState } from "./operators.ts";

// the same operator, typed the lazy way
function loadingStateAny(): OperatorFunction<any, any> {
  return (source) =>
    source.pipe(
      map((data) => ({ status: "success", data })),
      startWith({ status: "loading" }),
      catchError((error) => of({ status: "error", error })),
    );
}

of(books).pipe(loadingStateAny()).subscribe((state) => {
  // a typo in a property name: compiles, prints undefined
  if (state.status === "success") console.log("any:", state.data.lenght);
});

of(books).pipe(loadingState()).subscribe((state) => {
  // @ts-expect-error -- the generic version catches the same typo at compile time
  if (state.status === "success") console.log("generic:", state.data.lenght);
});
