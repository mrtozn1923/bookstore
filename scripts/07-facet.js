// 07 - $facet: one query for the search page: results, total and filter counts
const result = db.books.aggregate([
    { $match: { isActive: true, listPrice: { $lte: Decimal128("600") } } },
    { $facet: {
        results: [
            { $sort: { listPrice: 1, _id: 1 } },
            { $limit: 3 },
            { $project: { _id: 0, title: 1, listPrice: 1 } }
        ],
        total: [{ $count: "books" }],
        byCategory: [{ $sortByCount: { $ifNull: ["$category.name", "(none)"] } }, { $limit: 3 }],
        byLanguage: [{ $sortByCount: "$language" }],
        byPrice: [{ $bucket: { groupBy: "$listPrice", boundaries: [0, 300, 450, 601], default: "other",
                               output: { books: { $sum: 1 } } } }]
    } }
]).next();

print("results:", result.results.map(b => `${b.title} (${b.listPrice})`).join(", "));
print("total:", result.total[0].books);
print("byCategory:", result.byCategory.map(c => `${c._id} ${c.count}`).join(", "));
print("byLanguage:", result.byLanguage.map(c => `${c._id} ${c.count}`).join(", "));
print("byPrice:", result.byPrice.map(b => `[${b._id}..) ${b.books}`).join(", "));
