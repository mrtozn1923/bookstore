// 13 - resharding orders_by_date to a compound key: online, but it takes at least five minutes (admin, mongos)
const started = Date.now();
const result = db.adminCommand({ reshardCollection: "bookstore.orders_by_date", key: { "customer.customerId": 1, orderedAt: 1 } });
print("reshardCollection ok:", result.ok, "after", Math.round((Date.now() - started) / 1000), "s");
print("new shard key:", JSON.stringify(db.getSiblingDB("config").collections.findOne({ _id: "bookstore.orders_by_date" }).key));
db.getSiblingDB("bookstore").orders_by_date.aggregate([{ $collStats: { storageStats: {} } }])
    .forEach(s => print(s.shard, s.storageStats.count - (s.storageStats.numOrphanDocs ?? 0), "documents"));
