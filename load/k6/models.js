import http from 'k6/http';
import { check, sleep } from 'k6';

// Usage:
//   k6 run -e MODEL=closed -e VUS=200 models.js   (closed model: fixed number of users)
//   k6 run -e MODEL=open -e RATE=200 models.js    (open model: fixed arrival rate)
const BASE_URL = __ENV.BASE_URL || 'http://localhost:5080';
const MODEL = __ENV.MODEL || 'open';
const DURATION = __ENV.DURATION || '60s';
const THINK_TIME = Number(__ENV.THINK || 1);

const scenarios = {
    closed: {
        executor: 'constant-vus',
        vus: Number(__ENV.VUS || 200),
        duration: DURATION,
    },
    open: {
        executor: 'constant-arrival-rate',
        rate: Number(__ENV.RATE || 200),
        timeUnit: '1s',
        duration: DURATION,
        preAllocatedVUs: 50,
        maxVUs: Number(__ENV.MAX_VUS || 1000),
    },
};

export const options = {
    scenarios: { [MODEL]: scenarios[MODEL] },
    // Acceptance criteria derived from the SLO: p95 under 300 ms, under 0.5% errors.
    thresholds: {
        http_req_duration: ['p(95)<300'],
        http_req_failed: ['rate<0.005'],
    },
};

export function setup() {
    const res = http.get(`${BASE_URL}/api/books?pageSize=50`);
    return { isbns: res.json('items').map((b) => b.isbn) };
}

export default function (data) {
    const isbn = data.isbns[Math.floor(Math.random() * data.isbns.length)];
    const res = http.get(`${BASE_URL}/api/books/${isbn}`, { tags: { name: 'GET /api/books/{isbn}' } });
    check(res, { 'status is 200': (r) => r.status === 200 });
    if (MODEL === 'closed') {
        // Closed model: the user waits for the response, thinks (0.5-1.5 x THINK seconds), then sends the next request.
        sleep(THINK_TIME * (0.5 + Math.random()));
    }
}
