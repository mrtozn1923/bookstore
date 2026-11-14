// 11 - EF Core row version: existing books get version 0 (there are no EF Core migrations for MongoDB)
const result = db.books.updateMany(
    { version: { $exists: false } },
    { $set: { version: NumberLong("0") } }
);
print("books updated:", result.modifiedCount);
