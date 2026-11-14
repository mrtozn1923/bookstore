type Method = "GET" | "POST";

function request(url: string, method: Method) {
  console.log(`${method} ${url}`);
}

const options = {
  url: "/api/books",
  method: "GET",
} as const;
// options: { readonly url: "/api/books"; readonly method: "GET"; }

request(options.url, options.method); // no error now

const methods = ["GET", "POST"] as const;
// methods: readonly ["GET", "POST"]
