import { filter, map, Subject } from "rxjs";

// an app-wide "event bus": one Subject, string event types, payload of any shape
type BusEvent = { type: string; payload: unknown };
const bus = new Subject<BusEvent>();
const on = (type: string) =>
  bus.pipe(
    filter((e) => e.type === type),
    map((e) => e.payload),
  );

on("cart:added").subscribe((p) => console.log("header badge got", p));
on("cart:add").subscribe((p) => console.log("toast got", p)); // typo: compiles, never fires

bus.next({ type: "cart:added", payload: { isbn: "9780441172719", title: "Dune" } });
bus.next({ type: "cart:added", payload: "Emma" }); // a different payload shape, also compiles
