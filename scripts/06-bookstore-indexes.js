// 06 - the Bookstore index set, derived from the screens' queries (run after 02-load.sh)
db.orders.createIndex({ "customer.customerId": 1, orderedAt: -1 });              // my orders, newest first
db.orders.createIndex({ "items.bookId": 1 });                                     // orders containing a book (multikey)
db.orders.createIndex({ status: 1, orderedAt: 1 },
    { partialFilterExpression: { status: { $in: ["Pending", "Paid", "Shipped"] } } });   // open orders only
db.reviews.createIndex({ bookId: 1, reviewedAt: -1 });                            // a book's latest reviews
db.books.createIndex({ title: 1 }, { collation: { locale: "tr", strength: 2 } }); // case-insensitive title search
db.books.createIndex({ "authors.authorId": 1 });                                  // books of an author (multikey)
db.carts.createIndex({ customerId: 1 }, { unique: true });                        // one cart per customer
db.carts.createIndex({ updatedAt: 1 }, { expireAfterSeconds: 30 * 24 * 3600 });  // abandoned carts expire

for (const c of ["books", "orders", "reviews", "carts"]) {
    print(c.padEnd(8), db[c].getIndexes().map(i => i.name).join(", "));
}
