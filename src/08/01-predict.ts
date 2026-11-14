import { share, timer } from "rxjs";
import { fromFetch } from "rxjs/fetch";

// GET /api/categories once, shared by every subscriber... or is it?
const categories$ = fromFetch("http://localhost:5180/api/categories", {
  selector: (response) => response.json() as Promise<string[]>,
}).pipe(share());

categories$.subscribe((c) => console.log("menu:", c.join(", ")));
categories$.subscribe((c) => console.log("filter:", c.join(", ")));
// the footer is rendered one second later
timer(1000).subscribe(() => categories$.subscribe((c) => console.log("footer:", c.join(", "))));
