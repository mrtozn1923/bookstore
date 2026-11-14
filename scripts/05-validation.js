// 05 - schema validation: $jsonSchema, moderate ve strict (admin ya da dbAdmin yetkisiyle)
const bookstore = db.getSiblingDB("bookstore");
const level = bookstore.getCollectionInfos({ name: "reviews" })[0].options.validationLevel;
if (level === "constraint") {
    bookstore.runCommand({ collMod: "reviews", validationLevel: "strict" });
}
bookstore.runCommand({ collMod: "reviews", validator: {}, validationLevel: "off" });
bookstore.reviews.deleteMany({ "customer.customerId": 39, bookId: 26 });

// A review left over from an older application, rating stored as a string
bookstore.reviews.insertOne({ bookId: 26, customer: { customerId: 39, name: "Barbara Liskov" },
    rating: "5", reviewedAt: ISODate("2023-03-01T09:00:00Z") });

const reviewSchema = {
    bsonType: "object",
    required: ["bookId", "customer", "rating", "reviewedAt"],
    properties: {
        bookId: { bsonType: "int" },
        customer: {
            bsonType: "object",
            required: ["customerId", "name"],
            properties: { customerId: { bsonType: "int" }, name: { bsonType: "string", minLength: 1 } }
        },
        rating: { bsonType: "int", minimum: 1, maximum: 5 },
        body: { bsonType: "string", maxLength: 1000 },
        reviewedAt: { bsonType: "date" },
        helpfulCount: { bsonType: "int", minimum: 0 },
        helpfulVoters: { bsonType: "array", items: { bsonType: "int" } }
    }
};

function tryWrite(label, action) {
    try {
        const r = action();
        print(`${label}: ok`, r.modifiedCount !== undefined ? `(modified ${r.modifiedCount})` : "");
    } catch (e) {
        print(`${label}: ${e.name}: ${e.message}`);
        return e;
    }
}

for (const validationLevel of ["moderate", "strict"]) {
    bookstore.runCommand({ collMod: "reviews", validator: { $jsonSchema: reviewSchema }, validationLevel, validationAction: "error" });
    tryWrite(`${validationLevel.padEnd(8)} update legacy review`, () =>
        bookstore.reviews.updateOne({ "customer.customerId": 39, bookId: 26 }, { $set: { body: `A classic (${validationLevel}).` } }));
}

const e = tryWrite("insert rating 6", () => bookstore.reviews.insertOne({
    bookId: 26, customer: { customerId: 38, name: "Margaret Hamilton" }, rating: 6, reviewedAt: new Date() }));
printjson(e.errInfo.details.schemaRulesNotSatisfied);

print("invalid documents:", bookstore.reviews.countDocuments({ $nor: [{ $jsonSchema: reviewSchema }] }));
