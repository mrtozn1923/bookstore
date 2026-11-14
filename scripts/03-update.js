// 03 - update operators
db.books.updateOne({ _id: 26 }, { $unset: { tags: "", promo: "" }, $set: { listPrice: Decimal128("390.50") } });

let r = db.books.updateOne(
    { _id: 26 },
    {
        $set: { listPrice: Decimal128("351.45"), promo: { label: "SciFi Week", endsAt: ISODate("2026-07-07T00:00:00Z") } },
        $addToSet: { tags: { $each: ["classic", "space-opera", "classic"] } }
    }
);
print("matched", r.matchedCount, "modified", r.modifiedCount);
r = db.books.updateOne({ _id: 26 }, { $addToSet: { tags: "classic" } });
print("same $addToSet again -> modified", r.modifiedCount);
db.books.updateOne({ _id: 26 }, { $pull: { tags: "space-opera" }, $unset: { promo: "" } });
printjson(db.books.findOne({ _id: 26 }, { _id: 0, listPrice: 1, tags: 1, promo: 1 }));
db.books.updateOne({ _id: 26 }, { $unset: { tags: "" }, $set: { listPrice: Decimal128("390.50") } });

// Positional $: updates the first matching array element (pending order 379, book 20)
db.orders.updateOne({ _id: 379 }, { $set: { "items.0.quantity": 1, total: Decimal128("440.00") } });
db.orders.updateOne(
    { _id: 379, status: "Pending", "items.bookId": 20 },
    { $inc: { "items.$.quantity": 1, total: Decimal128("440.00") } }
);
printjson(db.orders.findOne({ _id: 379 }, { _id: 0, items: 1, total: 1 }));
db.orders.updateOne({ _id: 379 }, { $set: { "items.0.quantity": 1, total: Decimal128("440.00") } });

// Filtered positional $[it]: ALL elements that match (return request)
r = db.orders.updateOne(
    { _id: 372 },
    { $set: { "items.$[it].returnRequested": true } },
    { arrayFilters: [{ "it.bookId": { $in: [53, 43] } }] }
);
printjson(db.orders.findOne({ _id: 372 }, { _id: 0, items: { bookId: 1, quantity: 1, returnRequested: 1 } }));
db.orders.updateOne({ _id: 372 }, { $unset: { "items.$[].returnRequested": "" } });
