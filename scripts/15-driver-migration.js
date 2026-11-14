// 15 - data written by driver 2.x: decimal strings become Decimal128, DateTimeOffset arrays the 3.x document form
const prices = db.driver_upgrade.updateMany(
    { ListPrice: { $type: "string" } },
    [{ $set: { ListPrice: { $toDecimal: "$ListPrice" } } }]
);

// 2.x array: [local ticks, offset in minutes]; 3.x document: { DateTime (UTC), Ticks, Offset }
// $set with a document on an array field would apply it to every element, so the array is moved aside first
const ticks = { $toLong: { $arrayElemAt: ["$legacy", 0] } };
const offset = { $arrayElemAt: ["$legacy", 1] };
const dates = db.driver_upgrade.updateMany(
    { ListedAt: { $type: "array" } },
    [
        { $set: { legacy: "$ListedAt" } },
        { $unset: "ListedAt" },
        { $set: { ListedAt: {
            DateTime: { $toDate: { $subtract: [
                { $divide: [{ $subtract: [ticks, NumberLong("621355968000000000")] }, 10000] },
                { $multiply: [offset, 60000] }] } },
            Ticks: ticks,
            Offset: offset
        } } },
        { $unset: "legacy" }
    ]
);
print("ListPrice converted:", prices.modifiedCount, "| ListedAt converted:", dates.modifiedCount);
