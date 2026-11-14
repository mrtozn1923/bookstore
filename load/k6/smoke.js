import http from 'k6/http';
import { check, group, sleep } from 'k6';

// Smoke test: 1 VU, a few iterations. Question: does the script and the system work at all?
const BASE_URL = __ENV.BASE_URL || 'http://localhost:5080';

export const options = {
    vus: 1,
    iterations: 10,
    thresholds: {
        http_req_failed: ['rate==0'],
        checks: ['rate==1'],
    },
};

export default function () {
    let isbn;
    group('browse', () => {
        const list = http.get(`${BASE_URL}/api/books?pageSize=12`);
        check(list, { 'list is 200': (r) => r.status === 200 });
        isbn = list.json('items.0.isbn');
        const book = http.get(`${BASE_URL}/api/books/${isbn}`, { tags: { name: 'GET /api/books/{isbn}' } });
        check(book, { 'book is 200': (r) => r.status === 200 });
    });
    group('checkout', () => {
        const cart = http.post(`${BASE_URL}/api/carts`, null);
        check(cart, { 'cart is 201': (r) => r.status === 201 });
        const cartId = cart.json('id');
        const params = { headers: { 'Content-Type': 'application/json' } };
        const item = http.put(`${BASE_URL}/api/carts/${cartId}/items/${isbn}`, JSON.stringify({ quantity: 1 }),
            { ...params, tags: { name: 'PUT /api/carts/{id}/items/{isbn}' } });
        check(item, { 'item is 200': (r) => r.status === 200 });
        const order = http.post(`${BASE_URL}/api/orders`,
            JSON.stringify({ cartId, customerName: 'Ada Lovelace', customerEmail: 'ada@example.com' }), params);
        check(order, { 'order is 201': (r) => r.status === 201 });
    });
    sleep(1);
}
