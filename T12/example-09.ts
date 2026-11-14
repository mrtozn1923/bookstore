type Route = { path: string; title: string };

const routes = {
  catalog: { path: "/books", title: "Catalog" },
  cart: { path: "/cart", title: "Cart" },
} as const satisfies Record<string, Route>;

const catalogPath = routes.catalog.path; // type: "/books"
console.log(catalogPath); // /books
