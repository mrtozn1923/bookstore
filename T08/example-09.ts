type Method = "GET" | "POST";

type RequestOptions = {
  url: string;
  method: Method;
};

function request(url: string, method: Method) {
  console.log(`${method} ${url}`);
}

const options: RequestOptions = { url: "/api/books", method: "GET" };
request(options.url, options.method);
