type Method = "GET" | "POST";

function request(url: string, method: Method) {
  console.log(`${method} ${url}`);
}

const options = { url: "/api/books", method: "GET" };

// Error: Argument of type 'string' is not assignable to parameter of type 'Method'.
request(options.url, options.method);
