import http from 'k6/http';
import { check, sleep } from 'k6';

// Coordinated omission experiment: the same ~200 requests/s as a closed and as an open model,
// while the API is paused for a few seconds in the middle of the test (docker pause).
// Usage: k6 run -e MODEL=closed pause.js   |   k6 run -e MODEL=open pause.js
const BASE_URL = __ENV.BASE_URL || 'http://localhost:5080';
const MODEL = __ENV.MODEL || 'open';
const isbns = ['9780441172719', '9780261102217', '9780141439587', '9780451524935', '9780060850524'];

const scenarios = {
    // 20 users, each waits 100 ms after a response: ~200 requests/s while the API is fast.
    closed: { executor: 'constant-vus', vus: 20, duration: '60s' },
    open: { executor: 'constant-arrival-rate', rate: 200, timeUnit: '1s', duration: '60s', preAllocatedVUs: 50, maxVUs: 2000 },
};

export const options = { scenarios: { [MODEL]: scenarios[MODEL] } };

export default function () {
    const isbn = isbns[Math.floor(Math.random() * isbns.length)];
    const res = http.get(`${BASE_URL}/api/books/${isbn}`, { tags: { name: 'GET /api/books/{isbn}' } });
    check(res, { 'book 200': (r) => r.status === 200 });
    if (MODEL === 'closed') {
        sleep(0.1);
    }
}
