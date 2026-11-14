import { map, of } from "rxjs";

// an operator is a function: Observable in, Observable out
const toMinorUnits = map((price: number) => price * 100);
console.log(typeof toMinorUnits);

// calling it directly ...
toMinorUnits(of(450, 320)).subscribe((v) => console.log("direct call:", v));

// ... is exactly what pipe does for you
of(390).pipe(toMinorUnits).subscribe((v) => console.log("pipe:", v));

// one operator value, reused on two sources
const cart$ = of(450, 320);
const wishlist$ = of(280);
cart$.pipe(toMinorUnits).subscribe((v) => console.log("cart:", v));
wishlist$.pipe(toMinorUnits).subscribe((v) => console.log("wishlist:", v));
