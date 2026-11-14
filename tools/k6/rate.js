import http from 'k6/http';
import { check } from 'k6';

// usage: k6 run -e URL=... -e RATE=2000 rate.js  (fixed arrival rate, not a saturation test)
export const options = {
  scenarios: {
    fixed: {
      executor: 'constant-arrival-rate',
      rate: Number(__ENV.RATE),
      timeUnit: '1s',
      duration: '20s',
      preAllocatedVUs: 100,
    },
  },
};

export default function () {
  const res = http.get(__ENV.URL);
  check(res, { 'status is 200': (r) => r.status === 200 });
}
