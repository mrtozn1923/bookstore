// 04 - single-document atomicity: payment and status change together
db.orders.updateOne({ _id: 379 }, { $set: { status: "Pending", payments: [] } });

for (let attempt = 1; attempt <= 2; attempt++) {
    const r = db.orders.updateOne(
        { _id: 379, status: "Pending" },
        {
            $set: { status: "Paid" },
            $push: { payments: { method: "card", amount: Decimal128("440.00"), paidAt: ISODate("2026-06-30T08:12:00Z") } }
        }
    );
    print(`attempt ${attempt}: matched ${r.matchedCount}, modified ${r.modifiedCount}`);
}
printjson(db.orders.findOne({ _id: 379 }, { _id: 0, status: 1, total: 1, payments: 1 }));
db.orders.updateOne({ _id: 379 }, { $set: { status: "Pending", payments: [] } });
