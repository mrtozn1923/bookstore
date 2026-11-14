// 04 - mapping the relational schema one to one: joining the order detail with $lookup
db.orders_flat.drop();
db.order_items_flat.drop();
db.orders.find().forEach(o => {
    db.orders_flat.insertOne({ _id: o._id, customerId: o.customer.customerId, orderedAt: o.orderedAt, status: o.status });
    db.order_items_flat.insertMany(o.items.map(i => ({ orderId: o._id, bookId: i.bookId, quantity: i.quantity, unitPrice: i.unitPrice })));
});
print("orders_flat:", db.orders_flat.countDocuments(), " order_items_flat:", db.order_items_flat.countDocuments());

const pipeline = [
    { $match: { _id: 1 } },
    { $lookup: { from: "order_items_flat", localField: "_id", foreignField: "orderId", as: "items" } },
    { $lookup: { from: "customers", localField: "customerId", foreignField: "_id", as: "customer" } }
];

function lookupStats(label) {
    const e = db.orders_flat.explain("executionStats").aggregate(pipeline);
    const stages = e.stages ?? [];
    const lookups = stages.filter(s => s.$lookup).map(s => `${s.$lookup.from}: docsExamined ${s.totalDocsExamined}, collectionScans ${s.collectionScans}`);
    print(label);
    lookups.forEach(l => print("  " + l));
}
lookupStats("without index on orderId");
db.order_items_flat.createIndex({ orderId: 1 });
lookupStats("with index on orderId");

const single = db.orders.find({ _id: 1 }).explain("executionStats").executionStats;
print("embedded order: docsExamined", single.totalDocsExamined);
db.orders_flat.drop();
db.order_items_flat.drop();
