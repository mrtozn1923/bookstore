// 08 - guess first: 7-day moving average of daily orders, December 2025
// SQL Temelleri 14: avg_7_days on 2025-12-21 is 1.00 (days without orders count as 0)
db.orders.aggregate([
    { $match: { orderedAt: { $gte: ISODate("2025-12-01"), $lt: ISODate("2026-01-01") } } },
    { $group: { _id: { $dateTrunc: { date: "$orderedAt", unit: "day" } }, orders: { $sum: 1 } } },
    { $setWindowFields: {
        sortBy: { _id: 1 },
        output: {
            byDocuments: { $avg: "$orders", window: { documents: [-6, 0] } },
            byRange: { $avg: "$orders", window: { range: [-6, 0], unit: "day" } }
        }
    } },
    { $match: { _id: { $gte: ISODate("2025-12-20"), $lt: ISODate("2025-12-24") } } }
]).forEach(d => print(d._id.toISOString().slice(0, 10), "orders", d.orders,
    " documents:", d.byDocuments.toFixed(2), " range:", d.byRange.toFixed(2)));
