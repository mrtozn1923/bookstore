// 07 - $unwind: one document per array element
// Best sellers: units sold per book (cancelled orders excluded)
db.orders.aggregate([
    { $match: { status: { $ne: "Cancelled" } } },
    { $unwind: "$items" },
    { $group: { _id: "$items.bookId", title: { $first: "$items.title" }, units: { $sum: "$items.quantity" },
                revenue: { $sum: { $multiply: ["$items.quantity", "$items.unitPrice"] } } } },
    { $sort: { units: -1, _id: 1 } },
    { $limit: 3 }
]).forEach(b => print(String(b._id).padStart(3), b.title.padEnd(22), "units", b.units, " revenue", b.revenue.toString()));

// Books with an empty authors array disappear after $unwind unless preserveNullAndEmptyArrays is set
for (const preserve of [false, true]) {
    const n = db.books.aggregate([
        { $unwind: { path: "$authors", preserveNullAndEmptyArrays: preserve } },
        { $count: "n" }
    ]).next().n;
    print(`preserveNullAndEmptyArrays: ${preserve} -> ${n} documents (books: ${db.books.countDocuments()})`);
}
