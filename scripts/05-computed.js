// 05 - computed pattern: a review summary in the book
db.reviews.deleteMany({ "customer.customerId": 40, bookId: 26 });
db.books.updateMany({}, { $unset: { rating: "" } });

// Initial fill: compute once from reviews
db.reviews.aggregate([
    { $group: { _id: "$bookId", count: { $sum: 1 }, sum: { $sum: "$rating" } } }
]).forEach(r => db.books.updateOne(
    { _id: r._id },
    { $set: { rating: { count: r.count, sum: r.sum, average: Math.round(r.sum / r.count * 100) / 100 } } }
));
printjson(db.books.findOne({ _id: 26 }, { _id: 0, title: 1, rating: 1 }));

// New review: the review goes to its own collection, the summary to the book (pipeline update)
db.reviews.insertOne({ bookId: 26, customer: { customerId: 40, name: "Linus Torvalds" }, rating: 5,
    body: "Re-read it after twenty years.", reviewedAt: ISODate("2026-06-30T10:00:00Z") });
db.books.updateOne({ _id: 26 }, [
    { $set: { "rating.count": { $add: ["$rating.count", 1] }, "rating.sum": { $add: ["$rating.sum", 5] } } },
    { $set: { "rating.average": { $round: [{ $divide: ["$rating.sum", "$rating.count"] }, 2] } } }
]);
printjson(db.books.findOne({ _id: 26 }, { _id: 0, rating: 1 }));
print("books with rating:", db.books.countDocuments({ rating: { $exists: true } }));
