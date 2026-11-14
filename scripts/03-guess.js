// 03 - guess first: orders with at least 2 copies of Dune (bookId 26)
const dotted = db.orders.countDocuments({ "items.bookId": 26, "items.quantity": { $gte: 2 } });
const elemMatch = db.orders.countDocuments({ items: { $elemMatch: { bookId: 26, quantity: { $gte: 2 } } } });
print("dot notation:", dotted, " $elemMatch:", elemMatch);

// The extra matches: different elements satisfy the two conditions
printjson(db.orders.find(
    {
        "items.bookId": 26,
        "items.quantity": { $gte: 2 },
        items: { $not: { $elemMatch: { bookId: 26, quantity: { $gte: 2 } } } }
    },
    { _id: 1, "items.bookId": 1, "items.quantity": 1 }
).toArray());
