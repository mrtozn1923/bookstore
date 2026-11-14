// 08 - $setWindowFields: running total and ranking within a partition
// Monthly revenue and year-to-date total (SQL Temelleri 14: SUM(...) OVER (ORDER BY month ROWS UNBOUNDED PRECEDING))
db.orders.aggregate([
    { $match: { status: { $ne: "Cancelled" }, orderedAt: { $gte: ISODate("2025-01-01"), $lt: ISODate("2026-01-01") } } },
    { $group: { _id: { $dateTrunc: { date: "$orderedAt", unit: "month" } }, revenue: { $sum: "$total" } } },
    { $setWindowFields: {
        sortBy: { _id: 1 },
        output: { ytd: { $sum: "$revenue", window: { documents: ["unbounded", "current"] } } }
    } }
]).forEach(m => print(m._id.toISOString().slice(0, 7), m.revenue.toString().padStart(9), m.ytd.toString().padStart(10)));

// Top-N per group (SQL Temelleri 13): the best-selling book of each category by units
db.orders.aggregate([
    { $match: { status: { $ne: "Cancelled" } } },
    { $unwind: "$items" },
    { $group: { _id: "$items.bookId", units: { $sum: "$items.quantity" } } },
    { $lookup: { from: "books", localField: "_id", foreignField: "_id", as: "book" } },
    { $set: { category: { $first: "$book.category.name" }, title: { $first: "$book.title" } } },
    { $setWindowFields: { partitionBy: "$category", sortBy: { units: -1 }, output: { rank: { $rank: {} } } } },
    { $match: { rank: 1, category: { $in: ["Science Fiction", "Programming", "Dystopian"] } } },
    { $sort: { category: 1 } }
]).forEach(b => print(b.category.padEnd(16), b.title.padEnd(26), "units", b.units));
