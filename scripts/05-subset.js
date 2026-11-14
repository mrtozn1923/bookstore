// 05 - subset pattern: a copy of the latest 3 reviews for the book page
db.books.updateOne({ _id: 26 }, { $unset: { recentReviews: "" } });
const latest = db.reviews.find({ bookId: 26 }, { _id: 1, rating: 1, body: 1, "customer.name": 1, reviewedAt: 1 })
    .sort({ reviewedAt: -1 }).limit(3).toArray();
db.books.updateOne({ _id: 26 }, { $set: { recentReviews: latest } });

// New review: add it, sort, keep the first 3
const review = { _id: new ObjectId(), rating: 4, body: "Slow start, great ending.",
    customer: { name: "Ada Lovelace" }, reviewedAt: ISODate("2026-06-30T12:00:00Z") };
db.books.updateOne({ _id: 26 }, {
    $push: { recentReviews: { $each: [review], $sort: { reviewedAt: -1 }, $slice: 3 } }
});
printjson(db.books.findOne({ _id: 26 }).recentReviews.map(r => ({ by: r.customer.name, at: r.reviewedAt })));
print("book size:", bsonsize(db.books.findOne({ _id: 26 })), "bytes");
