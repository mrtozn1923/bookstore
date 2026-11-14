// 01 - guess first: the same field with two different BSON types
db = db.getSiblingDB("bookstore_try");
db.orders.drop();

db.orders.insertMany([
    { _id: 1, status: "Delivered", orderedAt: "2026-06-29T10:15:00Z" },
    { _id: 2, status: "Delivered", orderedAt: ISODate("2026-06-29T10:15:00Z") }
]);

const since = ISODate("2026-06-01T00:00:00Z");
printjson(db.orders.find({ orderedAt: { $gte: since } }).toArray());

// Answer: the BSON type of the field in each document
printjson(db.orders.aggregate([
    { $project: { orderedAtType: { $type: "$orderedAt" } } }
]).toArray());
