import { throwError } from "rxjs";
import { fromFetch } from "rxjs/fetch";

const apiUrl = "http://localhost:5180/api";

// 1) fromFetch is lazy: the request starts on subscribe
const dune$ = fromFetch(`${apiUrl}/books/9780441172719`, {
  selector: (response) => response.json(),
});
dune$.subscribe((book) => console.log("1) book:", book.title, book.price));

// 2) a 404 is not an error for fetch: response.json() fails on the empty body
const missing$ = fromFetch(`${apiUrl}/books/9780000000000`, {
  selector: (response) => response.json(),
});
missing$.subscribe({
  next: (book) => console.log("2) book:", book),
  error: (err: Error) => console.log("2) error:", err.name, "-", err.message),
});

// 3) check response.ok yourself and turn HTTP errors into error notifications
const checked$ = fromFetch(`${apiUrl}/books/9780000000000`, {
  selector: (response) =>
    response.ok ? response.json() : throwError(() => new Error(`HTTP ${response.status}`)),
});
checked$.subscribe({ error: (err: Error) => console.log("3) error:", err.message) });

// 4) unsubscribe before the response: the request is aborted
const sub = fromFetch(`${apiUrl}/books`).subscribe(() => console.log("4) never printed"));
setTimeout(() => sub.unsubscribe(), 50);
