// 04 - duplicated fields: how many documents change when a category is renamed?
const r = db.books.updateMany(
    { "category.categoryId": 3 },
    { $set: { "category.name": "Sci-Fi" } }
);
print("category rename:", r.matchedCount, "books");
db.categories.updateOne({ _id: 3 }, { $set: { name: "Sci-Fi" } });

// The price in an order item is a snapshot: it does not follow the book
const oldPrice = db.books.findOne({ _id: 47 }).listPrice;
db.books.updateOne({ _id: 47 }, { $set: { listPrice: Decimal128("320.00") } });
print("book 47 listPrice:", db.books.findOne({ _id: 47 }).listPrice,
      " order 1 item unitPrice:", db.orders.findOne({ _id: 1 }).items[0].unitPrice);
db.books.updateOne({ _id: 47 }, { $set: { listPrice: oldPrice } });

// Undo
db.books.updateMany({ "category.categoryId": 3 }, { $set: { "category.name": "Science Fiction" } });
db.categories.updateOne({ _id: 3 }, { $set: { name: "Science Fiction" } });

// Many-to-many: the reference for an author's books lives in the book
printjson(db.books.find({ "authors.authorId": 18 }, { _id: 0, title: 1 }).sort({ _id: 1 }).toArray());
printjson(db.authors.findOne({ _id: 18 }));
