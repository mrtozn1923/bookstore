// O16: examples that need a browser; paste these blocks into the DevTools console one by one
// (if a block uses a function from examples.js, paste that file first)

// --- example 1 ---
window.addEventListener("popstate", () => console.log("got popstate"));

history.pushState({ page: 2 }, "", "?page=2");
console.log(location.search);

// --- example 2 ---
const url = new URL("https://books.example.com:8080/books/dune?format=paperback&currency=TRY#reviews");

url.protocol;  // "https:"
url.hostname;  // "books.example.com"
url.port;      // "8080"
url.host;      // "books.example.com:8080"
url.origin;    // "https://books.example.com:8080"
url.pathname;  // "/books/dune"
url.search;    // "?format=paperback&currency=TRY"
url.hash;      // "#reviews"

// --- example 3 ---
const params = new URLSearchParams("q=jane austen&tag=classic&tag=romance");

params.get("q");           // "jane austen"
params.get("missing");     // null
params.getAll("tag");      // ["classic", "romance"]
params.has("q");           // true
params.size;               // 3

params.set("page", "2");   // replaces if present, adds if not
params.append("tag", "sale");
params.delete("q");
params.toString();         // "tag=classic&tag=romance&page=2&tag=sale"

for (const [key, value] of params) {
  console.log(key, value);
}

// --- example 4 ---
const link = new URL("https://example.com/books?page=1");
link.searchParams.set("page", "2");
link.searchParams.append("sort", "price");
console.log(link.href); // "https://example.com/books?page=2&sort=price"

const current = new URLSearchParams(location.search);
const page = Number(current.get("page") ?? 1);

// --- example 5 ---
window.addEventListener("hashchange", () => {
  console.log("New hash:", location.hash);
});

location.hash = "reviews"; // no reload, a new history entry is added

// --- example 6 ---
// history.pushState(state, unused, url)
history.pushState({ view: "catalog" }, "", "?view=catalog");
history.state;        // { view: "catalog" }

// While typing a filter, update the current entry instead of adding one per keystroke
history.replaceState({ view: "catalog", q: "dune" }, "", "?view=catalog&q=dune");

// --- example 7 ---
window.addEventListener("popstate", (event) => {
  console.log("Address:", location.search, "State:", event.state);
});

history.pushState({ page: 1 }, "", "?page=1");
history.pushState({ page: 2 }, "", "?page=2");
history.back(); // Address: ?page=1 State: { page: 1 }
history.back(); // Address: (first address) State: null
