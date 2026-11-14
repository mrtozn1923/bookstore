// The Cache API stores Request/Response pairs; it is not the HTTP cache
export async function cacheCatalog() {
  const cache = await caches.open("catalog-v1");
  await cache.add("/api/books"); // fetches and stores the response; fails on a non-2xx status

  const cached = await cache.match("/api/books");
  const books = await cached.json();
  console.log(`cached: ${books.length} books, ETag ${cached.headers.get("ETag")}, Cache-Control ${cached.headers.get("Cache-Control")}`);
  console.log("keys:", (await cache.keys()).map((request) => new URL(request.url).pathname).join(", "));
  console.log("caches:", (await caches.keys()).join(", "));

  // a query string makes a different key unless you ask to ignore it
  console.log("match with ?page=1:", Boolean(await cache.match("/api/books?page=1")));
  console.log("ignoreSearch:", Boolean(await cache.match("/api/books?page=1", { ignoreSearch: true })));
}
