// 03 - a cart with upsert; recently viewed with $push + $each + $slice
db.carts.deleteMany({ customerId: 3 });
for (const bookId of [26, 42]) {
    const r = db.carts.updateOne(
        { customerId: 3 },
        {
            $setOnInsert: { createdAt: ISODate("2026-06-30T09:00:00Z") },
            $push: { items: { bookId, quantity: 1 } }
        },
        { upsert: true }
    );
    print(`book ${bookId}: matched ${r.matchedCount}, upserted ${r.upsertedCount}`);
}
printjson(db.carts.findOne({ customerId: 3 }, { _id: 0 }));
db.carts.deleteMany({ customerId: 3 });

db.customers.updateOne({ _id: 3 }, { $unset: { recentlyViewed: "" } });
for (const bookId of [26, 3, 42, 64]) {
    db.customers.updateOne(
        { _id: 3 },
        { $push: { recentlyViewed: { $each: [bookId], $slice: -3 } } }
    );
}
printjson(db.customers.findOne({ _id: 3 }, { _id: 0, firstName: 1, recentlyViewed: 1 }));
db.customers.updateOne({ _id: 3 }, { $unset: { recentlyViewed: "" } });
