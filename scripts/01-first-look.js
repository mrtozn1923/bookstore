// 01 - document, collection, BSON types and ObjectId
db = db.getSiblingDB("bookstore_try");
db.reviews.drop();

print("collections before:", db.getCollectionNames().filter(n => n === "reviews").length);
const result = db.reviews.insertOne({
    bookId: 26,
    customer: { customerId: 3, name: "Grace Hopper" },
    rating: 5,
    body: "Still the best science fiction novel I have read.",
    reviewedAt: new Date("2026-06-29T10:15:00Z")
});
print("collections after:", db.getCollectionNames().filter(n => n === "reviews").length);
printjson(result);

const doc = db.reviews.findOne({ _id: result.insertedId });
print("bsonsize:", bsonsize(doc));
print("id time :", doc._id.getTimestamp().toISOString());

// BSON types of the fields (with the $type aggregation operator)
printjson(db.reviews.aggregate([
    { $match: { _id: result.insertedId } },
    { $project: {
        _id: { $type: "$_id" },
        bookId: { $type: "$bookId" },
        rating: { $type: "$rating" },
        customer: { $type: "$customer" },
        reviewedAt: { $type: "$reviewedAt" }
    } }
]).toArray());
