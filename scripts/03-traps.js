// 03 - traps: replaceOne and null versus a missing field
db.books.insertOne({ _id: 103, isbn: "9798900001036", title: "Northanger Abbey",
    authors: [{ authorId: 1, name: "Jane Austen" }], listPrice: Decimal128("280.00"),
    stock: { onHand: 12, reorderLevel: 5 } });

// Replacing the whole document when only the price should change
db.books.replaceOne({ _id: 103 }, { listPrice: Decimal128("260.00") });
printjson(db.books.findOne({ _id: 103 }));

try {
    db.books.updateOne({ _id: 103 }, { listPrice: Decimal128("250.00") });
} catch (e) {
    print(e.name + ":", e.message);
}
db.books.deleteOne({ _id: 103 });

// null matches both a null value and a missing field
print("subtitle: null      ->", db.books.countDocuments({ subtitle: null }));
print("subtitle: $exists   ->", db.books.countDocuments({ subtitle: { $exists: false } }));
print("subtitle: $type null->", db.books.countDocuments({ subtitle: { $type: "null" } }));
