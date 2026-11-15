const http = require('http');
const fs = require('fs');
const path = require('path');

const types = { '.html': 'text/html; charset=utf-8', '.pdf': 'application/pdf' };

// iki ayrı origin: mağaza sayfaları ve fatura dosyaları
function serve(root, port, extraHeaders = () => ({})) {
  return http.createServer((req, res) => {
    if (req.method === 'POST') {
      let body = '';
      req.on('data', c => (body += c));
      req.on('end', () => {
        console.log(`server: POST ${req.url} ${body}`);
        res.end('saved');
      });
      return;
    }
    const url = new URL(req.url, `http://${req.headers.host}`);
    const file = path.join(root, url.pathname === '/' ? 'book.html' : url.pathname);
    if (!file.startsWith(root) || !fs.existsSync(file)) {
      res.statusCode = 404;
      return res.end('not found');
    }
    res.writeHead(200, { 'Content-Type': types[path.extname(file)] ?? 'application/octet-stream', ...extraHeaders(url) });
    res.end(fs.readFileSync(file));
  }).listen(port, '127.0.0.1');
}

module.exports = () => [
  serve(path.join(__dirname, 'site'), 52113),
  serve(path.join(__dirname, 'files'), 52114, url =>
    url.searchParams.has('attachment') ? { 'Content-Disposition': 'attachment; filename="order-1042-invoice.pdf"' } : {}),
];
