// 08 - $densify + $fill: add the days without orders, then average over calendar days
db.orders.aggregate([
    { $match: { orderedAt: { $gte: ISODate("2025-12-01"), $lt: ISODate("2026-01-01") } } },
    { $group: { _id: { $dateTrunc: { date: "$orderedAt", unit: "day" } }, orders: { $sum: 1 } } },
    { $densify: { field: "_id", range: { step: 1, unit: "day", bounds: [ISODate("2025-12-01"), ISODate("2026-01-01")] } } },
    { $fill: { output: { orders: { value: 0 } } } },
    { $setWindowFields: {
        sortBy: { _id: 1 },
        output: { avg7: { $avg: "$orders", window: { range: [-6, 0], unit: "day" } } }
    } },
    { $match: { _id: { $gte: ISODate("2025-12-20"), $lt: ISODate("2025-12-24") } } }
]).forEach(d => print(d._id.toISOString().slice(0, 10), "orders", d.orders, " avg7:", d.avg7.toFixed(2)));
