// 05 - archive pattern: closed orders before 2025-07-01 move to orders_archive
db.orders_archive.drop();
const cutoff = ISODate("2025-07-01T00:00:00Z");
const filter = { status: { $in: ["Delivered", "Cancelled"] }, orderedAt: { $lt: cutoff } };

db.orders.aggregate([
    { $match: filter },
    { $merge: { into: "orders_archive", whenMatched: "keepExisting", whenNotMatched: "insert" } }
]);
const archivedIds = db.orders_archive.distinct("_id");
const r = db.orders.deleteMany({ _id: { $in: archivedIds }, ...filter });
print("archived:", db.orders_archive.countDocuments(), " deleted:", r.deletedCount, " orders left:", db.orders.countDocuments());

// Undo: move them back for the next posts
db.orders_archive.aggregate([{ $merge: { into: "orders", whenMatched: "keepExisting", whenNotMatched: "insert" } }]);
db.orders_archive.drop();
print("orders after undo:", db.orders.countDocuments());
