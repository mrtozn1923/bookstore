// 06 - finding unused and redundant indexes; hiding before dropping (needs dbAdmin)
const bookstore = db.getSiblingDB("bookstore");
bookstore.orders.dropIndexes();
bookstore.orders.createIndex({ "customer.customerId": 1, orderedAt: -1 });
bookstore.orders.createIndex({ "customer.customerId": 1 });          // prefix of the index above
bookstore.orders.createIndex({ status: 1 });

// Some traffic: "my orders" and a status count
for (let i = 0; i < 20; i++) bookstore.orders.find({ "customer.customerId": 1 + i }).sort({ orderedAt: -1 }).limit(10).toArray();
bookstore.orders.countDocuments({ status: "Shipped" });

// $indexStats: how many times each index was used since the last restart
bookstore.orders.aggregate([{ $indexStats: {} }, { $project: { name: 1, ops: "$accesses.ops" } }, { $sort: { name: 1 } }])
    .forEach(s => print(s.name.padEnd(36), "ops", Number(s.ops)));

// Redundant: an index whose keys are a prefix of another index's keys
const indexes = bookstore.orders.getIndexes();
for (const a of indexes) for (const b of indexes) {
    const ka = Object.entries(a.key), kb = Object.entries(b.key);
    if (a.name !== b.name && ka.length < kb.length && ka.every(([k, v], i) => kb[i][0] === k && kb[i][1] === v)
        && !a.unique && !a.partialFilterExpression && !a.collation) {
        print("redundant:", a.name, "is a prefix of", b.name);
    }
}

// Hide first, watch the application, drop later (unhideIndex brings it back at once)
bookstore.orders.hideIndex("customer.customerId_1");
const e = bookstore.orders.find({ "customer.customerId": 1 }).explain();
print("hidden:", bookstore.orders.getIndexes().find(i => i.name === "customer.customerId_1").hidden,
      " plan uses:", e.queryPlanner.winningPlan.inputStage.indexName);
bookstore.orders.dropIndex("customer.customerId_1");
bookstore.orders.dropIndexes();
