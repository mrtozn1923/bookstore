import { from, of, range } from "rxjs";
import { books } from "../../data/books.ts";

const log = (label: string) => (value: unknown) => console.log(label, value);

of("Dune", "Emma").subscribe(log("of:"));
of(["Dune", "Emma"]).subscribe(log("of(array):")); // a single value: the array itself
from(["Dune", "Emma"]).subscribe(log("from(array):"));

// any iterable works: Map, Set, string, generator
const stockByTitle = new Map(books.slice(0, 2).map((b) => [b.title, b.stock] as const));
from(stockByTitle).subscribe(log("from(Map):"));

function* catalogPages() {
  yield "page 1";
  yield "page 2";
}
from(catalogPages()).subscribe(log("from(generator):"));

range(1, 3).subscribe(log("range:"));

// Promise and async iterable: asynchronous even when the value is ready
from(Promise.resolve(770)).subscribe(log("from(Promise):"));

async function* orderFeed() {
  yield 1041;
  yield 1042;
}
from(orderFeed()).subscribe(log("from(async iterable):"));

console.log("--- end of synchronous code ---");
