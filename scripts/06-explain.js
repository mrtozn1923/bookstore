// 06 - explain("executionStats"): COLLSCAN, IXSCAN and a covered query
db.orders.dropIndexes();

function summary(label, explain) {
    const s = explain.executionStats;
    const stages = [];
    for (let st = s.executionStages; st; st = st.inputStage) stages.push(st.stage);
    print(`${label.padEnd(10)} ${stages.join(" <- ").padEnd(36)} keys ${String(s.totalKeysExamined).padStart(3)}  docs ${String(s.totalDocsExamined).padStart(3)}  returned ${s.nReturned}`);
}

const filter = { "customer.customerId": 17 };
summary("no index", db.orders.find(filter).explain("executionStats"));

db.orders.createIndex({ "customer.customerId": 1, orderedAt: -1 });
summary("index", db.orders.find(filter).explain("executionStats"));

// Only indexed fields in the filter and projection, _id excluded: the documents are not read
summary("covered", db.orders.find(filter, { _id: 0, orderedAt: 1 }).explain("executionStats"));

// Prefix rule: the index starts with customer.customerId; orderedAt alone cannot use it
summary("no prefix", db.orders.find({ orderedAt: { $gte: ISODate("2026-06-01") } }).explain("executionStats"));
db.orders.dropIndexes();
