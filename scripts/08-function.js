// 08 - server-side JavaScript with $function versus a native expression: ISBN-13 check digit
const viaFunction = db.books.aggregate([
    { $set: { valid: { $function: {
        body: function (isbn) {
            let sum = 0;
            for (let i = 0; i < 12; i++) sum += Number(isbn[i]) * (i % 2 === 0 ? 1 : 3);
            return (10 - sum % 10) % 10 === Number(isbn[12]);
        },
        args: ["$isbn"],
        lang: "js"
    } } } },
    { $group: { _id: "$valid", books: { $sum: 1 } } }
]).toArray();
print("$function:", JSON.stringify(viaFunction));

// The same rule with aggregation operators: no JavaScript engine involved
const viaOperators = db.books.aggregate([
    { $set: { sum: { $reduce: {
        input: { $range: [0, 12] },
        initialValue: 0,
        in: { $add: ["$$value", { $multiply: [
            { $toInt: { $substrCP: ["$isbn", "$$this", 1] } },
            { $cond: [{ $eq: [{ $mod: ["$$this", 2] }, 0] }, 1, 3] }] }] }
    } } } },
    { $set: { valid: { $eq: [{ $mod: [{ $subtract: [10, { $mod: ["$sum", 10] }] }, 10] },
                              { $toInt: { $substrCP: ["$isbn", 12, 1] } }] } } },
    { $group: { _id: "$valid", books: { $sum: 1 } } }
]).toArray();
print("operators:", JSON.stringify(viaOperators));
