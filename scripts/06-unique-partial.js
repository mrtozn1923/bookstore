// 06 - unique, partial and sparse indexes
db.carts.drop();
db.carts.createIndex({ customerId: 1 }, { unique: true });
db.carts.insertOne({ customerId: 3, items: [{ bookId: 26, quantity: 1 }] });
try {
    db.carts.insertOne({ customerId: 3, items: [{ bookId: 42, quantity: 1 }] });
} catch (err) {
    print(err.message);
}
// With the unique index an upsert on customerId cannot create a second cart
const r = db.carts.updateOne({ customerId: 3 }, { $push: { items: { bookId: 42, quantity: 1 } } }, { upsert: true });
print(`upsert: matched ${r.matchedCount}, upserted ${r.upsertedCount}, carts ${db.carts.countDocuments()}`);
db.carts.drop();

function summary(label, explain) {
    const s = explain.executionStats;
    const stages = [];
    for (let st = s.executionStages; st; st = st.inputStage) stages.push(st.stage);
    print(`${label.padEnd(16)} ${stages.join(" <- ").padEnd(24)} keys ${String(s.totalKeysExamined).padStart(3)}  docs ${String(s.totalDocsExamined).padStart(3)}  returned ${s.nReturned}`);
}

// Partial index: only open orders are indexed (the warehouse screen)
db.orders.dropIndexes();
db.orders.createIndex({ status: 1, orderedAt: 1 },
    { partialFilterExpression: { status: { $in: ["Pending", "Paid", "Shipped"] } } });
summary("index size", db.orders.find({ status: { $in: ["Pending", "Paid", "Shipped"] } })
    .hint({ status: 1, orderedAt: 1 }).explain("executionStats"));
summary("status Shipped", db.orders.find({ status: "Shipped" }).sort({ orderedAt: 1 }).explain("executionStats"));
summary("status Delivered", db.orders.find({ status: "Delivered" }).sort({ orderedAt: 1 }).explain("executionStats"));
db.orders.dropIndexes();

// Sparse index: documents without the field are not in the index
db.books.createIndex({ subtitle: 1 }, { sparse: true });
summary("subtitle exists", db.books.find({ subtitle: { $exists: true } }).explain("executionStats"));
summary("subtitle null", db.books.find({ subtitle: null }).explain("executionStats"));
db.books.dropIndex({ subtitle: 1 });
