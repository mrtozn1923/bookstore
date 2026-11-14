type RouteParams<Path> =
  Path extends `${string}:${infer Param}/${infer Rest}`
    ? Param | RouteParams<`/${Rest}`>
    : Path extends `${string}:${infer Param}`
      ? Param
      : never;

type P1 = RouteParams<"/customers/:customerId/orders/:orderId">; // "customerId" | "orderId"
type P2 = RouteParams<"/books">; // never

function buildPath<P extends string>(path: P, params: Record<RouteParams<P>, string>): string {
  return path.replace(/:(\w+)/g, (_, name: string) => params[name as RouteParams<P>]);
}

console.log(buildPath("/customers/:customerId/orders/:orderId", { customerId: "7", orderId: "1042" })); // /customers/7/orders/1042

// Error: Property 'isbn' is missing in type '{}' but required in type 'Record<"isbn", string>'.
buildPath("/books/:isbn", {});
