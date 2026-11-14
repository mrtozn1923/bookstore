// A tiny back-office page; the API address comes from Aspire
const http = require("http");
const port = process.env.PORT || 3000;
http.createServer((req, res) => {
  res.writeHead(200, { "Content-Type": "text/plain" });
  res.end(`Bookstore back office. API: ${process.env.services__apiservice__http__0}\n`);
}).listen(port, () => console.log(`admin listening on ${port}`));
