// 03 - conditional $inc: stock never goes negative; findOneAndUpdate returns the new value
db.books.updateOne({ _id: 26 }, { $set: { "stock.onHand": 3 } });

for (const quantity of [2, 2]) {
    const r = db.books.updateOne(
        { _id: 26, "stock.onHand": { $gte: quantity } },
        { $inc: { "stock.onHand": -quantity } }
    );
    print(`take ${quantity}: matched ${r.matchedCount}, modified ${r.modifiedCount}`);
}

const after = db.books.findOneAndUpdate(
    { _id: 26, "stock.onHand": { $gte: 1 } },
    { $inc: { "stock.onHand": -1 }, $currentDate: { "stock.updatedAt": true } },
    { returnDocument: "after", projection: { _id: 0, "stock.onHand": 1 } }
);
printjson(after);
print("next call:", db.books.findOneAndUpdate(
    { _id: 26, "stock.onHand": { $gte: 1 } },
    { $inc: { "stock.onHand": -1 } },
    { returnDocument: "after" }
));
db.books.updateOne({ _id: 26 }, { $set: { "stock.onHand": 51, "stock.updatedAt": ISODate("2026-06-14T17:53:00Z") } });
