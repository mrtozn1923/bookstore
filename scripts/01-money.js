// 01 - double versus Decimal128
db = db.getSiblingDB("bookstore_try");
db.prices.drop();
db.prices.insertMany([
    { book: "Dune", asDouble: 0.1, asDecimal: Decimal128("0.1") },
    { book: "Emma", asDouble: 0.2, asDecimal: Decimal128("0.2") }
]);
printjson(db.prices.aggregate([
    { $group: { _id: null, doubleSum: { $sum: "$asDouble" }, decimalSum: { $sum: "$asDecimal" } } }
]).toArray());
