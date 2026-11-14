function defineRoutes<T extends readonly string[]>(paths: T): T {
  return paths;
}

function defineConstRoutes<const T extends readonly string[]>(paths: T): T {
  return paths;
}

const routes = defineRoutes(["/books", "/cart"]); // string[]
const constRoutes = defineConstRoutes(["/books", "/cart"]); // readonly ["/books", "/cart"]
