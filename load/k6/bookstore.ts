import http from 'k6/http';
import { check, group, sleep } from 'k6';
import { SharedArray } from 'k6/data';
import { expect } from 'https://jslib.k6.io/k6-testing/0.6.1/index.js';

// Bookstore average-load test: one open-model scenario per user flow, so every flow keeps its own rate.
// Usage: k6 run -e SCALE=1 bookstore.ts   (SCALE=2 doubles every rate)
const BASE_URL = __ENV.BASE_URL || 'http://localhost:5080';
const SCALE = Number(__ENV.SCALE || 1);
const DURATION = __ENV.DURATION || '5m';

const isbns = new SharedArray('isbns', () =>
    open('../data/isbns.txt').split(/\r?\n/).filter((line: string) => line.length === 13));
const terms = ['river', 'garden', 'kingdom', 'silent', 'lighthouse', 'library', 'winter', 'dune', 'austen', 'turing'];

function flow(exec: string, sessionsPerSecond: number, secondsPerSession: number) {
    const rate = sessionsPerSecond * SCALE;
    return {
        executor: 'ramping-arrival-rate',
        exec,
        startRate: 0,
        timeUnit: '1s',
        // Little's Law with 50% headroom, so k6 does not have to create VUs during the test.
        preAllocatedVUs: Math.ceil(rate * secondsPerSession * 1.5),
        maxVUs: 500,
        stages: [
            { target: rate, duration: '30s' },  // ramp-up (warm-up)
            { target: rate, duration: DURATION },
        ],
        tags: { flow: exec },
    };
}

export const options = {
    scenarios: {
        browse: flow('browse', 14, 9),
        search: flow('search', 4, 3.5),
        checkout: flow('checkout', 2, 13.5),
    },
    thresholds: {
        // Acceptance criteria from the SLO, per endpoint where it matters.
        'http_req_duration{name:GET /api/books/{isbn}}': ['p(95)<100'],
        'http_req_duration{name:GET /api/books?search}': ['p(95)<300'],
        'http_req_duration{name:POST /api/orders}': ['p(95)<500'],
        http_req_failed: [{ threshold: 'rate<0.005', abortOnFail: true, delayAbortEval: '30s' }],
        checks: ['rate>0.995'],
        // A dropped iteration is a session we wanted to start but could not: not more than 1 per 1,000.
        dropped_iterations: ['count<30'],
    },
};

type SetupData = { categories: string[] };

export function setup(): SetupData {
    // Fail fast: if the environment is wrong, an assertion stops the test before any load is sent.
    const health = http.get(`${BASE_URL}/api/categories`);
    expect(health.status).toBe(200);
    const categories = (health.json() as { slug: string }[]).map((c) => c.slug);
    expect(categories.length).toBeGreaterThan(0);
    const book = http.get(`${BASE_URL}/api/books/${isbns[isbns.length - 1]}`);
    expect(book.status, 'test data is loaded (run seed-catalog.sql)').toBe(200);
    return { categories };
}

function pickIsbn(): string {
    return isbns[Math.floor(Math.pow(Math.random(), 3) * isbns.length)];
}

function think(min: number, max: number) {
    sleep(min + Math.random() * (max - min));
}

function viewBook(isbn: string) {
    const res = http.get(`${BASE_URL}/api/books/${isbn}`, { tags: { name: 'GET /api/books/{isbn}' } });
    check(res, { 'book 200': (r) => r.status === 200 });
}

export function browse(data: SetupData) {
    group('browse', () => {
        const category = data.categories[Math.floor(Math.random() * data.categories.length)];
        const list = http.get(`${BASE_URL}/api/books?category=${category}&page=${1 + Math.floor(Math.random() * 5)}`,
            { tags: { name: 'GET /api/books' } });
        check(list, { 'list 200': (r) => r.status === 200 });
        think(2, 5);
        viewBook(pickIsbn());
        think(3, 8);
        viewBook(pickIsbn());
    });
}

export function search() {
    group('search', () => {
        const term = terms[Math.floor(Math.random() * terms.length)];
        const res = http.get(`${BASE_URL}/api/books?search=${term}`, { tags: { name: 'GET /api/books?search' } });
        check(res, { 'search 200': (r) => r.status === 200 });
        think(2, 5);
        viewBook(pickIsbn());
    });
}

export function checkout() {
    group('checkout', () => {
        const isbn = pickIsbn();
        viewBook(isbn);
        think(2, 5);
        const json = { headers: { 'Content-Type': 'application/json' } };
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
    });
}
