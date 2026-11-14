// 03 - "helpful" vote: the same customer is not counted twice
const review = db.reviews.findOne({ bookId: 26, body: { $exists: true } }, { _id: 1 });
db.reviews.updateOne({ _id: review._id }, { $unset: { helpfulVoters: "", helpfulCount: "" } });

for (const customerId of [7, 7, 12]) {
    const r = db.reviews.updateOne(
        { _id: review._id, helpfulVoters: { $ne: customerId } },
        { $addToSet: { helpfulVoters: customerId }, $inc: { helpfulCount: 1 } }
    );
    print(`customer ${customerId}: modified ${r.modifiedCount}`);
}
printjson(db.reviews.findOne({ _id: review._id }, { _id: 0, helpfulVoters: 1, helpfulCount: 1 }));
db.reviews.updateOne({ _id: review._id }, { $unset: { helpfulVoters: "", helpfulCount: "" } });
