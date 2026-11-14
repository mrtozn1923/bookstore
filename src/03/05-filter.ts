import { filter, map, skip, takeWhile } from "rxjs";
import { showMarbles } from "../../tools/marble.ts";

type AddToCart = { type: "add"; title: string; price: number };
type RemoveFromCart = { type: "remove"; title: string };
type CartEvent = AddToCart | RemoveFromCart;

const events: Record<string, CartEvent> = {
  a: { type: "add", title: "Dune", price: 450 },
  b: { type: "remove", title: "Dune" },
  c: { type: "add", title: "Bookmark", price: 0 }, // a free gift added by a campaign
  d: { type: "add", title: "The Hobbit", price: 390 },
};

// an explicit type guard: after this filter, TypeScript knows every value is AddToCart
const isPaidAdd = (e: CartEvent): e is AddToCart => e.type === "add" && e.price > 0;

showMarbles("filter(isPaidAdd), map((e) => e.price)", (cold) =>
  cold("-a--b--c--d--|", events).pipe(
    filter(isPaidAdd),
    map((e) => e.price),
  ),
);

// Middlemarch's stock while orders come in
const stock = { a: 5, b: 3, c: 2, d: 4 };
showMarbles("takeWhile((s) => s >= 3)", (cold) =>
  cold("-a--b--c--d--|", stock).pipe(takeWhile((s) => s >= 3)),
);
showMarbles("takeWhile((s) => s >= 3, true)", (cold) =>
  cold("-a--b--c--d--|", stock).pipe(takeWhile((s) => s >= 3, true)),
);
showMarbles("skip(1)", (cold) => cold("-a--b--c--d--|", stock).pipe(skip(1)));
