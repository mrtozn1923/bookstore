// 02 - load check: document counts and BSON types
for (const name of ["categories", "authors", "books", "customers", "orders", "reviews"]) {
    print(name.padEnd(11), db.getCollection(name).countDocuments());
}
printjson(db.books.findOne({ _id: 26 }));
printjson(db.orders.aggregate([
    { $match: { _id: 1 } },
    { $project: {
        orderedAt: { $type: "$orderedAt" },
        total: { $type: "$total" },
        quantity: { $type: { $first: "$items.quantity" } },
        items: { $size: "$items" }
    } }
]).toArray());
