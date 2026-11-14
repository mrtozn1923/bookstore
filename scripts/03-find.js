// 03 - find: filter, projection, sort and limit
printjson(db.books.find(
    { "category.name": "Science Fiction", listPrice: { $lt: Decimal128("500") } },
    { _id: 0, title: 1, listPrice: 1, "authors.name": 1 }
).sort({ listPrice: -1 }).limit(3).toArray());

print("$in      :", db.books.countDocuments({ "category.name": { $in: ["Dystopian", "Science Fiction"] } }));
print("not ex.  :", db.books.countDocuments({ publishedYear: { $exists: false } }));

// $regex: case-insensitive search; without an index every document is scanned
const regexStats = db.books.find({ title: { $regex: "^the", $options: "i" } })
    .explain("executionStats").executionStats;
print("regex    : returned", regexStats.nReturned, "docsExamined", regexStats.totalDocsExamined,
      "stage", regexStats.executionStages.stage);

// skip: skipped entries are still read (ordered read through the _id index)
for (const skip of [0, 60, 300]) {
    const s = db.orders.find({}, { status: 1 }).sort({ _id: -1 }).skip(skip).limit(20)
        .explain("executionStats").executionStats;
    print(`skip ${String(skip).padStart(3)} : returned ${s.nReturned}, keysExamined ${s.totalKeysExamined}, docsExamined ${s.totalDocsExamined}`);
}
