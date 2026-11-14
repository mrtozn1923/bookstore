// 07 - $group: books per category, HAVING with $match, accumulators
// SQL Temelleri 10: GROUP BY category_id with COUNT, MIN, MAX
db.books.aggregate([
    { $group: {
        _id: "$category.categoryId",
        name: { $first: "$category.name" },
        books: { $sum: 1 },
        cheapest: { $min: "$listPrice" },
        priciest: { $max: "$listPrice" },
        top: { $top: { sortBy: { listPrice: -1 }, output: "$title" } }
    } },
    { $sort: { books: -1, _id: 1 } },
    { $limit: 4 }
]).forEach(c => print(String(c._id).padStart(4), (c.name ?? "-").padEnd(16), String(c.books).padStart(3),
    c.cheapest.toString().padStart(8), c.priciest.toString().padStart(8), " ", c.top));

// HAVING: a $match after $group filters the groups
print("customers with 20+ orders:");
db.orders.aggregate([
    { $group: { _id: "$customer.customerId", name: { $first: "$customer.name" }, orders: { $sum: 1 } } },
    { $match: { orders: { $gte: 20 } } },
    { $sort: { orders: -1 } }
]).forEach(c => print(" ", String(c._id).padStart(2), c.name.padEnd(18), c.orders));

// $avg of int values returns a double
printjson(db.reviews.aggregate([
    { $group: { _id: null, avg: { $avg: "$rating" }, total: { $sum: "$rating" }, reviews: { $count: {} } } },
    { $project: { _id: 0, avg: { $round: ["$avg", 6] }, total: 1, reviews: 1 } }
]).next());
