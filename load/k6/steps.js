import http from 'k6/http';
import { check } from 'k6';
import { SharedArray } from 'k6/data';

// Stepped arrival rate for the throughput-latency curve (breakpoint style): every step holds one rate.
// Usage: k6 run -e START=100 -e STEP=100 -e STEPS=10 -e STEP_DURATION=20s steps.js
const BASE_URL = __ENV.BASE_URL || 'http://localhost:5080';
const START = Number(__ENV.START || 100);
const STEP = Number(__ENV.STEP || 100);
const STEPS = Number(__ENV.STEPS || 10);
const STEP_DURATION = __ENV.STEP_DURATION || '20s';
const isbns = new SharedArray('isbns', () => open('../data/isbns.txt').split(/\r?\n/).filter((l) => l.length === 13));

const stages = [];
for (let i = 0; i < STEPS; i++) {
    const rate = START + i * STEP;
    stages.push({ target: rate, duration: '1s' });          // jump to the next rate
    stages.push({ target: rate, duration: STEP_DURATION }); // hold it
}

export const options = {
    scenarios: {
        steps: {
            executor: 'ramping-arrival-rate',
            startRate: START,
            timeUnit: '1s',
            preAllocatedVUs: 50,
            maxVUs: 2000,
            stages,
        },
    },
};

export default function () {
    // A unique query string bypasses the output cache: every request reaches the API and the database.
    const isbn = isbns[Math.floor(Math.random() * isbns.length)];
    const res = http.get(`${BASE_URL}/api/books/${isbn}?run=${__VU}-${__ITER}`, { tags: { name: 'GET /api/books/{isbn}' } });
    check(res, { 'book 200': (r) => r.status === 200 });
}
