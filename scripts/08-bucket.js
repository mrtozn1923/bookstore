// 08 - $bucketAuto: price ranges with roughly the same number of books
db.books.aggregate([
    { $bucketAuto: { groupBy: "$listPrice", buckets: 4, output: { books: { $sum: 1 } } } }
]).forEach(b => print(`${b._id.min} - ${b._id.max}`.padEnd(18), "books", b.books));
