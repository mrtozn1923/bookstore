import http from 'k6/http';
import { check } from 'k6';
import { SharedArray } from 'k6/data';

// Same endpoint, same rate, three different ways to pick the book.
// Usage: k6 run -e POPULARITY=single|zipf|uniform -e RATE=600 book-details.js
const BASE_URL = __ENV.BASE_URL || 'http://localhost:5080';
const POPULARITY = __ENV.POPULARITY || 'zipf';
const isbns = new SharedArray('isbns', () => open('../data/isbns.txt').split(/\r?\n/).filter((l) => l.length === 13));

export const options = {
    scenarios: {
        details: {
            executor: 'constant-arrival-rate',
            rate: Number(__ENV.RATE || 600),
            timeUnit: '1s',
            duration: __ENV.DURATION || '60s',
            preAllocatedVUs: 50,
            maxVUs: 1000,
        },
    },
};

function pickIsbn() {
    if (POPULARITY === 'single') return isbns[0];
    const r = POPULARITY === 'uniform' ? Math.random() : Math.pow(Math.random(), 3);
    return isbns[Math.floor(r * isbns.length)];
}

export default function () {
    const res = http.get(`${BASE_URL}/api/books/${pickIsbn()}`, { tags: { name: 'GET /api/books/{isbn}' } });
    check(res, { 'book 200': (r) => r.status === 200 });
}
