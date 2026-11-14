import http from 'k6/http';
import { check } from 'k6';

// usage: k6 run -e URL=http://localhost:5300/api/catalog/books/9780441172719 saturate.js
export const options = { vus: 50, duration: '20s' };

export default function () {
  const res = http.get(__ENV.URL);
  check(res, { 'status is 200': (r) => r.status === 200 });
}
