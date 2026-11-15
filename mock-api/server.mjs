// In-memory stand-in for Bookstore.Api (same routes and JSON shapes), plus the built Angular app.
// Usage (from the lab root): node mock-api/server.mjs [port]   (default 52411, so it does not clash with Bookstore.Api on 5080)
import { createServer } from 'node:http';
import { randomUUID } from 'node:crypto';
import { readFile } from 'node:fs/promises';
import { existsSync, readFileSync } from 'node:fs';
import { extname, join, normalize, resolve } from 'node:path';

const host = process.env.HOST ?? '127.0.0.1';
const port = Number(process.argv[2] ?? process.env.PORT ?? 52411);
const dist = resolve(process.env.DIST ?? 'web/bookstore-web/dist/bookstore-web/browser');
const catalog = JSON.parse(readFileSync(new URL('./catalog.json', import.meta.url), 'utf8'));
const carts = new Map();
const orders = [];
const types = { '.html': 'text/html; charset=utf-8', '.js': 'text/javascript', '.css': 'text/css', '.ico': 'image/x-icon', '.svg': 'image/svg+xml' };

const categoryName = slug => catalog.categories.find(c => c.slug === slug)?.name ?? slug;
const summary = b => ({ isbn: b.isbn, title: b.title, author: b.author, category: categoryName(b.category), price: b.price, publishedYear: b.publishedYear });
const round = n => Math.round(n * 100) / 100;

function cartView(cart) {
  const items = [...cart.items.entries()]
    .map(([isbn, quantity]) => {
      const b = catalog.books.find(x => x.isbn === isbn);
      return { isbn, title: b.title, unitPrice: b.price, quantity, lineTotal: round(b.price * quantity) };
    })
    .sort((a, b) => a.title.localeCompare(b.title));
  return { id: cart.id, items, total: round(items.reduce((s, l) => s + l.lineTotal, 0)), currency: 'TRY' };
}

function send(res, status, body) {
  res.writeHead(status, { 'Content-Type': status >= 400 && body?.title ? 'application/problem+json' : 'application/json' });
  res.end(body === undefined ? '' : JSON.stringify(body));
}

async function readJson(req) {
  let raw = '';
  for await (const chunk of req) raw += chunk;
  return raw ? JSON.parse(raw) : null;
}

async function api(req, res, url) {
  const parts = url.pathname.split('/').filter(Boolean).slice(1);
  const [resource, id, sub, isbn] = parts;
  if (req.method === 'GET' && resource === 'categories') {
    return send(res, 200, catalog.categories);
  }
  if (req.method === 'GET' && resource === 'books' && !id) {
    const term = (url.searchParams.get('search') ?? '').trim().toLowerCase();
    const category = url.searchParams.get('category') ?? '';
    const pageSize = Math.min(Math.max(Number(url.searchParams.get('pageSize') ?? 12), 1), 50);
    const page = Math.max(Number(url.searchParams.get('page') ?? 1), 1);
    const hits = catalog.books
      .filter(b => !term || b.title.toLowerCase().includes(term) || b.author.toLowerCase().includes(term) || b.isbn === term)
      .filter(b => !category || b.category === category)
      .sort((a, b) => a.title.localeCompare(b.title));
    return send(res, 200, { items: hits.slice((page - 1) * pageSize, page * pageSize).map(summary), total: hits.length, page, pageSize });
  }
  if (req.method === 'GET' && resource === 'books') {
    const b = catalog.books.find(x => x.isbn === id);
    return b ? send(res, 200, { ...summary(b), description: b.description, available: b.stock }) : send(res, 404);
  }
  if (resource === 'carts') {
    if (req.method === 'POST' && !id) {
      const cart = { id: randomUUID(), items: new Map() };
      carts.set(cart.id, cart);
      return send(res, 201, cartView(cart));
    }
    const cart = carts.get(id);
    if (!cart) return send(res, 404);
    if (req.method === 'GET' && !sub) return send(res, 200, cartView(cart));
    if (req.method === 'PUT' && sub === 'items') {
      if (!catalog.books.some(b => b.isbn === isbn)) return send(res, 404);
      cart.items.set(isbn, (await readJson(req)).quantity);
      return send(res, 200, cartView(cart));
    }
    if (req.method === 'DELETE' && sub === 'items') {
      cart.items.delete(isbn);
      return send(res, 200, cartView(cart));
    }
  }
  if (req.method === 'POST' && resource === 'orders') {
    const body = await readJson(req);
    const errors = {};
    if (!body?.customerName?.trim()) errors.CustomerName = ['The CustomerName field is required.'];
    if (!/^[^@\s]+@[^@\s]+$/.test(body?.customerEmail ?? '')) errors.CustomerEmail = ['The CustomerEmail field is not a valid e-mail address.'];
    if (Object.keys(errors).length) return send(res, 400, { title: 'One or more validation errors occurred.', status: 400, errors });
    const cart = carts.get(body.cartId);
    if (!cart) return send(res, 404);
    const view = cartView(cart);
    if (!view.items.length) return send(res, 400, { title: 'Bad Request', status: 400, detail: 'The cart is empty.' });
    const short = view.items.filter(l => catalog.books.find(b => b.isbn === l.isbn).stock < l.quantity).map(l => l.isbn);
    if (short.length) return send(res, 409, { title: 'Conflict', status: 409, detail: `Not enough stock for: ${short.join(', ')}.` });
    const order = { id: orders.length + 1, status: 'Placed', placedAtUtc: new Date().toISOString(), items: view.items, total: view.total, currency: 'TRY' };
    orders.push(order);
    carts.delete(cart.id);
    return send(res, 201, order);
  }
  return send(res, 404);
}

async function staticFile(res, url) {
  const file = normalize(join(dist, decodeURIComponent(url.pathname)));
  const target = file.startsWith(dist) && extname(file) && existsSync(file) ? file : join(dist, 'index.html');
  res.writeHead(200, { 'Content-Type': types[extname(target)] ?? 'application/octet-stream' });
  res.end(await readFile(target));
}

const server = createServer(async (req, res) => {
  const url = new URL(req.url, `http://${host}`);
  try {
    if (url.pathname.startsWith('/api/')) return await api(req, res, url);
    return await staticFile(res, url);
  } catch (error) {
    console.error(error);
    send(res, 500, { title: 'Internal Server Error', status: 500 });
  }
});
server.listen(port, host, () => console.log(`Bookstore mock API on http://${host}:${port}`));
