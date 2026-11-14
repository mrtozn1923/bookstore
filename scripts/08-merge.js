// 08 - $merge: a daily sales summary as a materialized view, refreshed on demand
db.daily_sales.drop();
db.orders.deleteMany({ _id: 380 });

function refresh(from) {
    db.orders.aggregate([
        { $match: { status: { $ne: "Cancelled" }, orderedAt: { $gte: from } } },
        { $group: { _id: { $dateTrunc: { date: "$orderedAt", unit: "day" } },
                    orders: { $sum: 1 }, revenue: { $sum: "$total" } } },
        { $set: { refreshedAt: "$$NOW" } },
        { $merge: { into: "daily_sales", on: "_id", whenMatched: "replace", whenNotMatched: "insert" } }
    ]);
}
const day = ISODate("2026-06-29");
const show = label => {
    const d = db.daily_sales.findOne({ _id: day });
    print(label.padEnd(22), "days", db.daily_sales.countDocuments(), " 2026-06-29:", d.orders, "orders", d.revenue.toString());
};

refresh(ISODate("2000-01-01"));                       // full build
show("full build");

// A new order arrives; only the affected days are recomputed
db.orders.insertOne({ _id: 380, customer: { customerId: 3, name: "Grace Hopper" }, orderedAt: ISODate("2026-06-29T22:10:00Z"),
    status: "Pending", items: [{ bookId: 26, title: "Dune", quantity: 1, unitPrice: Decimal128("390.50") }],
    total: Decimal128("390.50"), currency: "TRY", payments: [] });
show("before refresh");
refresh(ISODate("2026-06-29"));
show("incremental refresh");
refresh(ISODate("2026-06-29"));
show("same refresh again");

// Reading the report is now a small indexed read
db.daily_sales.find({}, { refreshedAt: 0 }).sort({ _id: -1 }).limit(3)
    .forEach(d => print(d._id.toISOString().slice(0, 10), d.orders, d.revenue.toString()));

// Undo for the next posts
db.orders.deleteOne({ _id: 380 });
db.daily_sales.drop();
