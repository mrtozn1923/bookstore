import http from 'k6/http';
import { check, sleep } from 'k6';
import { SharedArray } from 'k6/data';

// Bookstore workload model: sessions arrive at RATE per second (open model);
// 70% browse, 20% search, 10% checkout. Each session is several requests with think time.
// Usage: k6 run -e RATE=20 -e DURATION=5m bookstore-mix.js
const BASE_URL = __ENV.BASE_URL || 'http://localhost:5080';
const THINK = Number(__ENV.THINK || 1);           // think time multiplier (0 = no think time)
const POPULARITY = __ENV.POPULARITY || 'zipf';    // zipf | uniform | single

// 40,000 ISBNs exported from the test database; loaded once and shared by all VUs.
const isbns = new SharedArray('isbns', () => open('../data/isbns.txt').split(/\r?\n/).filter((l) => l.length === 13));
const categories = ['classics', 'science-fiction', 'fantasy', 'dystopian'];
const searchTerms = ['river', 'garden', 'kingdom', 'silent', 'lighthouse', 'library', 'winter', 'dune', 'austen',
    'turing', 'hopper', 'map', 'storm', 'tolkien', 'machine', 'island', 'orwell', 'voyage', 'archive', 'golden'];

export const options = {
    scenarios: {
        sessions: {
            executor: 'constant-arrival-rate',
            rate: Number(__ENV.RATE || 20),
            timeUnit: '1s',
            duration: __ENV.DURATION || '5m',
            // Little's Law: 20 sessions/s x ~8.5 s per session = ~170 concurrent sessions.
            preAllocatedVUs: 200,
            maxVUs: 1000,
        },
    },
};

// A few books are viewed far more often than the rest (long tail): x^3 puts ~46% of views on the top 10%.
function pickIsbn() {
    if (POPULARITY === 'single') return isbns[0];
    const r = POPULARITY === 'uniform' ? Math.random() : Math.pow(Math.random(), 3);
    return isbns[Math.floor(r * isbns.length)];
}

function think(min, max) {
    sleep(THINK * (min + Math.random() * (max - min)));
}

const json = { headers: { 'Content-Type': 'application/json' } };

function viewBook(isbn) {
    const res = http.get(`${BASE_URL}/api/books/${isbn}`, { tags: { name: 'GET /api/books/{isbn}' } });
    check(res, { 'book 200': (r) => r.status === 200 });
}

function browse() {
    const category = categories[Math.floor(Math.random() * categories.length)];
    const page = 1 + Math.floor(Math.random() * 5);
    const list = http.get(`${BASE_URL}/api/books?category=${category}&page=${page}`, { tags: { name: 'GET /api/books' } });
    check(list, { 'list 200': (r) => r.status === 200 });
    think(2, 5);
    viewBook(pickIsbn());
    think(3, 8);
    viewBook(pickIsbn());
}

function search() {
    const term = searchTerms[Math.floor(Math.random() * searchTerms.length)];
    const res = http.get(`${BASE_URL}/api/books?search=${term}`, { tags: { name: 'GET /api/books?search' } });
    check(res, { 'search 200': (r) => r.status === 200 });
    think(2, 5);
    viewBook(pickIsbn());
}

function checkout() {
    const isbn = pickIsbn();
    viewBook(isbn);
    think(2, 5);
    const cart = http.post(`${BASE_URL}/api/carts`, null, { tags: { name: 'POST /api/carts' } });
    check(cart, { 'cart 201': (r) => r.status === 201 });
    const cartId = cart.json('id');
    http.put(`${BASE_URL}/api/carts/${cartId}/items/${isbn}`, JSON.stringify({ quantity: 1 }),
        { ...json, tags: { name: 'PUT /api/carts/{id}/items/{isbn}' } });
    think(5, 15);
    const order = http.post(`${BASE_URL}/api/orders`,
        JSON.stringify({ cartId, customerName: 'Grace Hopper', customerEmail: `grace.${__VU}@example.com` }),
        { ...json, tags: { name: 'POST /api/orders' } });
    check(order, { 'order 201': (r) => r.status === 201 });
}

export default function () {
    const r = Math.random();
    if (r < 0.7) browse();
    else if (r < 0.9) search();
    else checkout();
}
