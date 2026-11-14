// 07 - stage order: the optimizer moves $match forward so it can use an index (run 06-bookstore-indexes.js first)
function plan(e) {
    const qp = e.queryPlanner ?? e.stages[0].$cursor.queryPlanner;
    const stages = [];
    for (let n = qp.winningPlan.queryPlan ?? qp.winningPlan; n; n = n.inputStage) stages.push(n.stage);
    const rest = e.stages ? e.stages.slice(1).map(s => Object.keys(s)[0]) : [];
    return stages.join(" <- ") + (rest.length ? "  then " + rest.join(", ") : "");
}

// Written with $set first and $match second
const pipeline = [
    { $set: { year: { $year: "$orderedAt" } } },
    { $match: { "customer.customerId": 1 } },
    { $group: { _id: "$year", orders: { $sum: 1 }, spent: { $sum: "$total" } } },
    { $sort: { _id: 1 } }
];
print("plan:", plan(db.orders.explain().aggregate(pipeline)));
db.orders.aggregate(pipeline).forEach(r => print(" ", r._id, "orders", r.orders, "spent", r.spent));

// A $match on a field computed in the pipeline cannot move before the stage that computes it
print("plan:", plan(db.orders.explain().aggregate([
    { $set: { year: { $year: "$orderedAt" } } },
    { $match: { year: 2026 } },
    { $count: "orders" }
])));
