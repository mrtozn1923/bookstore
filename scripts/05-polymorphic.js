// 05 - polymorphic pattern: print, ebook and audiobook editions in one collection
db.editions.drop();
db.editions.insertMany([
    { _id: "26-print", bookId: 26, format: "print", price: Decimal128("390.50"),
      binding: "paperback", pageCount: 513, weightGrams: 520 },
    { _id: "26-ebook", bookId: 26, format: "ebook", price: Decimal128("210.00"),
      fileFormats: ["epub", "pdf"], drm: false },
    { _id: "26-audio", bookId: 26, format: "audiobook", price: Decimal128("320.00"),
      narrator: "Grace Hopper", durationMinutes: 1263 }
]);

// One query on the shared fields; format-specific fields only where they apply
printjson(db.editions.find({ bookId: 26 }, { _id: 0, format: 1, price: 1 }).sort({ price: 1 }).toArray());
print("ebooks without DRM:", db.editions.countDocuments({ format: "ebook", drm: false }));

// Attribute pattern: searchable attributes in one k/v array, one index
db.editions.updateMany({}, [{ $set: { attributes: {
    $filter: {
        input: { $objectToArray: "$$ROOT" },
        cond: { $in: ["$$this.k", ["binding", "pageCount", "drm", "narrator", "durationMinutes"]] }
    }
} } }]);
db.editions.createIndex({ "attributes.k": 1, "attributes.v": 1 });
const stats = db.editions.find({ attributes: { $elemMatch: { k: "narrator", v: "Grace Hopper" } } })
    .explain("executionStats");
print("narrator query:", stats.queryPlanner.winningPlan.inputStage?.stage ?? stats.queryPlanner.winningPlan.stage,
      "keysExamined", stats.executionStats.totalKeysExamined, "returned", stats.executionStats.nReturned);
printjson(db.editions.findOne({ _id: "26-audio" }, { _id: 0, attributes: 1 }));
