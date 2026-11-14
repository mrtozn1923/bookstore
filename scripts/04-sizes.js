// 04 - document sizes per collection ($bsonSize)
for (const name of ["books", "orders", "reviews", "customers"]) {
    const [s] = db.getCollection(name).aggregate([
        { $group: { _id: null, n: { $sum: 1 },
                    avg: { $avg: { $bsonSize: "$$ROOT" } }, max: { $max: { $bsonSize: "$$ROOT" } } } }
    ]).toArray();
    print(`${name.padEnd(9)} n=${String(s.n).padStart(3)}  avg=${Math.round(s.avg)} B  max=${s.max} B`);
}

// Reviews per book and items per order
const [r] = db.reviews.aggregate([
    { $group: { _id: "$bookId", n: { $sum: 1 } } },
    { $group: { _id: null, books: { $sum: 1 }, avg: { $avg: "$n" }, max: { $max: "$n" } } }
]).toArray();
print(`reviews per book: books=${r.books} avg=${r.avg.toFixed(1)} max=${r.max}`);
const [o] = db.orders.aggregate([
    { $group: { _id: null, avg: { $avg: { $size: "$items" } }, max: { $max: { $size: "$items" } } } }
]).toArray();
print(`items per order : avg=${o.avg.toFixed(2)} max=${o.max}`);
