// Görsel testler için build edilmiş Angular uygulamasını sunan küçük bir statik sunucu
import { createServer } from 'node:http';
import { readFile } from 'node:fs/promises';
import { extname, join } from 'node:path';
const [root, port] = process.argv.slice(2);
const types = { '.html': 'text/html', '.js': 'text/javascript', '.css': 'text/css', '.ico': 'image/x-icon' };
createServer(async (req, res) => {
  const path = new URL(req.url, 'http://x').pathname;
  let file = join(root, path === '/' ? 'index.html' : path);
  let body;
  try { body = await readFile(file); } catch { file = join(root, 'index.html'); body = await readFile(file); }
  res.writeHead(200, { 'content-type': types[extname(file)] ?? 'application/octet-stream' });
  res.end(body);
}).listen(Number(port), () => console.log('static on', port));
