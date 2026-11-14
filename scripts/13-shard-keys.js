// 13 - a monotonic range key versus a hashed key: where new orders go and which queries are targeted (admin, mongos)
const bookstore = db.getSiblingDB("bookstore");
db.getSiblingDB("config").settings.updateOne({ _id: "chunksize" }, { $set: { value: 1 } }, { upsert: true });   // 1 MB: small lab data still splits

sh.shardCollection("bookstore.orders_by_date", { orderedAt: 1 });
sh.shardCollection("bookstore.orders_by_customer", { "customer.customerId": "hashed" });

// Documents per shard; a migration leaves orphan copies behind for a while, they are not counted
const countsPerShard = name => Object.fromEntries(bookstore[name]
    .aggregate([{ $collStats: { storageStats: {} } }]).toArray()
    .map(s => [s.shard, s.storageStats.count - (s.storageStats.numOrphanDocs ?? 0)]));

// 20,000 orders built from order 1; orderedAt only grows, like real traffic
const sample = bookstore.orders.findOne({ _id: 1 });
const start = ISODate("2026-07-01T00:00:00Z").getTime();
const order = i => Object.assign({}, sample, {
    _id: 1000 + i,
    customer: { customerId: (i * 7919) % 40 + 1, name: "customer" },
    orderedAt: new Date(start + i * 60 * 1000)
});

for (const name of ["orders_by_date", "orders_by_customer"]) {
    for (let batch = 0; batch < 9; batch++) {
        bookstore[name].insertMany(Array.from({ length: 2000 }, (_, k) => order(batch * 2000 + k)));
    }
    sleep(15000);   // give the balancer a chance to move data
    const before = countsPerShard(name);
    bookstore[name].insertMany(Array.from({ length: 2000 }, (_, k) => order(18000 + k)));
    const after = countsPerShard(name);
    print(`${name}: last 2,000 inserts went to`,
          Object.keys(after).sort().map(s => `${s} +${after[s] - (before[s] ?? 0)}`).join(", "),
          "| documents per shard:", Object.keys(after).sort().map(s => `${s} ${after[s]}`).join(", "));
}

// Targeted or scatter-gather? The shards each query is sent to
function shardsFor(name, filter) {
    const plan = bookstore[name].find(filter).explain().queryPlanner.winningPlan;
    return `${plan.stage} [${(plan.shards ?? []).map(s => s.shardName).sort().join(", ")}]`;
}
const lastDay = { orderedAt: { $gte: new Date(start + 19000 * 60 * 1000) } };
const customer = { "customer.customerId": 17 };
for (const name of ["orders_by_date", "orders_by_customer"]) {
    print(name.padEnd(19), "last day:", shardsFor(name, lastDay).padEnd(32), "customer 17:", shardsFor(name, customer));
}
