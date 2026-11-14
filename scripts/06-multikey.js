// 06 - multikey index: one index key per array element
db.orders.dropIndexes();
db.orders.createIndex({ "items.bookId": 1 });

const e = db.orders.find({ "items.bookId": 26 }).explain("executionStats");
const ix = e.queryPlanner.winningPlan.inputStage;
print("stage", ix.stage, " isMultiKey", ix.isMultiKey, " multiKeyPaths", JSON.stringify(ix.multiKeyPaths),
      " keys", e.executionStats.totalKeysExamined, " returned", e.executionStats.nReturned);

// A full scan of the index visits every key once: more keys than documents
const full = db.orders.find().hint({ "items.bookId": 1 }).explain("executionStats").executionStats;
print("orders", full.nReturned, " index keys", full.totalKeysExamined);

// $elemMatch uses the same index; both conditions are checked on the same element
const em = db.orders.find({ items: { $elemMatch: { bookId: 26, quantity: { $gte: 2 } } } }).explain("executionStats");
print("$elemMatch keys", em.executionStats.totalKeysExamined, " docs", em.executionStats.totalDocsExamined, " returned", em.executionStats.nReturned);

// Two array fields in one compound index are not allowed ("parallel arrays")
try {
    db.orders.createIndex({ "items.bookId": 1, "payments.method": 1 });
} catch (err) {
    print(err.codeName + ":", err.message.split(" :: caused by :: ").pop());
}
db.orders.dropIndexes();
