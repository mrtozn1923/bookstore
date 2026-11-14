// 07 - $lookup: revenue by category (orders -> books), latest reviews, join strategy (run 06-bookstore-indexes.js first)
// The order item has no category: look up the book for it
db.orders.aggregate([
    { $match: { status: { $ne: "Cancelled" }, orderedAt: { $gte: ISODate("2025-01-01"), $lt: ISODate("2026-01-01") } } },
    { $unwind: "$items" },
    { $lookup: { from: "books", localField: "items.bookId", foreignField: "_id", as: "book" } },
    { $unwind: "$book" },
    { $group: { _id: { $ifNull: ["$book.category.name", "(none)"] },
                revenue: { $sum: { $multiply: ["$items.quantity", "$items.unitPrice"] } } } },
    { $sort: { revenue: -1 } },
    { $limit: 3 }
]).forEach(c => print(c._id.padEnd(16), c.revenue.toString().padStart(10)));

// Pipeline $lookup: only the latest 2 reviews of each book, only the fields the page needs
printjson(db.books.aggregate([
    { $match: { _id: 26 } },
    { $lookup: {
        from: "reviews",
        localField: "_id",
        foreignField: "bookId",
        pipeline: [
            { $sort: { reviewedAt: -1 } },
            { $limit: 2 },
            { $project: { _id: 0, rating: 1, by: "$customer.name" } }
        ],
        as: "latestReviews"
    } },
    { $project: { _id: 0, title: 1, latestReviews: 1 } }
]).next());

// How is the join executed? With and without an index on the foreign field
function lookupStrategy(e) {
    const c = e.stages ? e.stages[0].$cursor : e;
    const find = n => !n ? null : n.stage === "EQ_LOOKUP" ? n : find(n.inputStage) ?? (n.inputStages ?? []).map(find).find(x => x);
    const node = find(c.queryPlanner.winningPlan.queryPlan ?? c.queryPlanner.winningPlan);
    return `${node.strategy}  docsExamined ${c.executionStats.totalDocsExamined}  keysExamined ${c.executionStats.totalKeysExamined}`;
}
const allBooks = [{ $lookup: { from: "reviews", localField: "_id", foreignField: "bookId", as: "reviews" } }];
print("with index   :", lookupStrategy(db.books.explain("executionStats").aggregate(allBooks)));
db.reviews.dropIndex({ bookId: 1, reviewedAt: -1 });
print("without index:", lookupStrategy(db.books.explain("executionStats").aggregate(allBooks)));
db.reviews.createIndex({ bookId: 1, reviewedAt: -1 });
