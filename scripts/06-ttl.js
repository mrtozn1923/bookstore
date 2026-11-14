// 06 - TTL indexes: abandoned carts and expiring stock reservations
db.carts.drop();
db.stock_reservations.drop();

// Sliding expiry: a cart is deleted 30 days after its last change
db.carts.createIndex({ updatedAt: 1 }, { expireAfterSeconds: 30 * 24 * 3600 });
const day = 24 * 3600 * 1000;
db.carts.insertMany([
    { customerId: 3, items: [{ bookId: 26, quantity: 1 }], updatedAt: new Date(Date.now() - 45 * day) },
    { customerId: 17, items: [{ bookId: 42, quantity: 1 }], updatedAt: new Date() }
]);

// Fixed expiry: each reservation carries its own expiry time (expireAfterSeconds: 0)
db.stock_reservations.createIndex({ expiresAt: 1 }, { expireAfterSeconds: 0 });
db.stock_reservations.insertMany([
    { bookId: 26, orderId: 380, quantity: 1, expiresAt: new Date(Date.now() - 60 * 1000) },
    { bookId: 26, orderId: 381, quantity: 2, expiresAt: new Date(Date.now() + 15 * 60 * 1000) },
    { bookId: 26, orderId: 382, quantity: 1, expiresAt: "2026-06-30T10:00:00Z" }     // a string, not a date
]);

print("before: carts", db.carts.countDocuments(), " reservations", db.stock_reservations.countDocuments());
const started = Date.now();
while (db.carts.countDocuments() > 1 || db.stock_reservations.countDocuments() > 2) {
    if (Date.now() - started > 150 * 1000) { print("TTL monitor did not run within 150 s"); break; }
    sleep(1000);
}
print("after the TTL pass: carts", db.carts.countDocuments(), " reservations", db.stock_reservations.countDocuments());
printjson(db.stock_reservations.aggregate([
    { $project: { _id: 0, orderId: 1, expiresAtType: { $type: "$expiresAt" } } }, { $sort: { orderId: 1 } }
]).toArray());
db.carts.drop();
db.stock_reservations.drop();
