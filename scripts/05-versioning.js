// 05 - schema versioning: customers v1 (city, country) -> v2 (address)
// Undo an earlier run
db.customers.updateMany({ schemaVersion: 2 }, [
    { $set: { city: "$address.city", country: "$address.country" } },
    { $unset: ["address", "schemaVersion"] }
]);

// New code writes v2: new and updated customers
db.customers.updateOne({ _id: 3 }, [
    { $set: { schemaVersion: 2, address: { city: "$city", country: "$country" } } },
    { $unset: ["city", "country"] }
]);

// Reads support both shapes
printjson(db.customers.aggregate([
    { $match: { _id: { $in: [2, 3] } } },
    { $project: {
        _id: 1,
        schemaVersion: { $ifNull: ["$schemaVersion", 1] },
        city: { $ifNull: ["$address.city", "$city"] }
    } }
]).toArray());

// Background migration: small batches, safe to resume
let migrated = 0;
while (true) {
    const ids = db.customers.find({ schemaVersion: { $exists: false } }, { _id: 1 }).limit(15).toArray().map(c => c._id);
    if (ids.length === 0) break;
    const r = db.customers.updateMany({ _id: { $in: ids }, schemaVersion: { $exists: false } }, [
        { $set: { schemaVersion: 2, address: { city: "$city", country: "$country" } } },
        { $unset: ["city", "country"] }
    ]);
    migrated += r.modifiedCount;
    print("batch:", r.modifiedCount);
}
print("migrated:", migrated, " v1 left:", db.customers.countDocuments({ schemaVersion: { $exists: false } }));
printjson(db.customers.findOne({ _id: 2 }, { _id: 0, firstName: 1, schemaVersion: 1, address: 1 }));
