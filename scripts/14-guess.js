// 14 - guess first: what can the application user do besides reading and writing documents?
db.books.aggregate([{ $match: {} }, { $out: "books_copy" }]);
print("books_copy documents:", db.books_copy.countDocuments());
print("drop() as bookstore_app:", db.books_copy.drop());
