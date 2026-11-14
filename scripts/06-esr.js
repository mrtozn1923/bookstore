// 06 - ESR: equality, sort, range; the same query with three index orders
db.orders.dropIndexes();
db.orders.createIndex({ "customer.customerId": 1, orderedAt: -1, total: 1 }, { name: "esr" });
db.orders.createIndex({ "customer.customerId": 1, total: 1, orderedAt: -1 }, { name: "ers" });
db.orders.createIndex({ total: 1, "customer.customerId": 1, orderedAt: -1 }, { name: "range_first" });

function summary(label, explain) {
    const s = explain.executionStats;
    const stages = [];
    for (let st = s.executionStages; st; st = st.inputStage) stages.push(st.stage);
    print(`${label.padEnd(12)} ${stages.join(" <- ").padEnd(30)} keys ${String(s.totalKeysExamined).padStart(3)}  docs ${String(s.totalDocsExamined).padStart(3)}  returned ${s.nReturned}`);
}

// Ada's (customer 1) latest 3 orders of at least 1000 TRY
for (const min of ["1000", "3000"]) {
    print(`total >= ${min}`);
    for (const name of ["esr", "ers", "range_first"]) {
        summary(name, db.orders.find({ "customer.customerId": 1, total: { $gte: Decimal128(min) } })
            .sort({ orderedAt: -1 }).limit(3).hint(name).explain("executionStats"));
    }
}

// Which plan does the optimizer pick on its own?
const auto = db.orders.find({ "customer.customerId": 1, total: { $gte: Decimal128("1000") } })
    .sort({ orderedAt: -1 }).limit(3).explain("executionStats");
print("chosen:", auto.queryPlanner.winningPlan.inputStage?.indexName ?? auto.queryPlanner.winningPlan.inputStage?.inputStage?.indexName,
      " rejected plans:", auto.queryPlanner.rejectedPlans.length);

// Three single-field indexes instead of one compound index
db.orders.dropIndexes();
db.orders.createIndex({ "customer.customerId": 1 });
db.orders.createIndex({ total: 1 });
db.orders.createIndex({ orderedAt: -1 });
summary("singles", db.orders.find({ "customer.customerId": 1, total: { $gte: Decimal128("1000") } })
    .sort({ orderedAt: -1 }).limit(3).explain("executionStats"));

// $in with a sort: a short list behaves like equality (SORT_MERGE), a long one like a range (SORT)
db.orders.dropIndexes();
db.orders.createIndex({ status: 1, orderedAt: -1 });
const open = ["Pending", "Paid", "Shipped"];
const padded = open.concat(Array.from({ length: 198 }, (_, i) => `Unused${i}`));
for (const list of [open, padded]) {
    summary(`$in ${list.length}`, db.orders.find({ status: { $in: list } }).sort({ orderedAt: -1 }).limit(5).explain("executionStats"));
}
db.orders.dropIndexes();
