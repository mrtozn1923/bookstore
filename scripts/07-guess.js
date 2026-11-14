// 07 - guess first: monthly revenue, Q1 2025 (SQL Temelleri 14: January 8959.20)
const months = { $match: { status: { $ne: "Cancelled" },
    orderedAt: { $gte: ISODate("2025-01-01"), $lt: ISODate("2025-04-01") } } };
const month = { $dateTrunc: { date: "$orderedAt", unit: "month" } };

const a = db.orders.aggregate([
    months,
    { $unwind: "$items" },
    { $group: { _id: month, revenue: { $sum: "$total" } } },
    { $sort: { _id: 1 } }
]).toArray();

const b = db.orders.aggregate([
    months,
    { $group: { _id: month, revenue: { $sum: "$total" } } },
    { $sort: { _id: 1 } }
]).toArray();

a.forEach((r, i) => print(r._id.toISOString().slice(0, 7), " A:", r.revenue.toString().padStart(9), " B:", b[i].revenue.toString().padStart(9)));
