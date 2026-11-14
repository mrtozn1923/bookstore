// 05 - MongoDB 9.0: constraint validation level
const bookstore = db.getSiblingDB("bookstore");

function tryCommand(label, action) {
    try {
        const r = action();
        if (r.writeErrors) {
            print(`${label}: inserted ${r.n}, writeError code ${r.writeErrors[0].code}: ${r.writeErrors[0].errmsg}`);
        } else {
            print(`${label}: ok`);
        }
    } catch (e) {
        print(`${label}: ${e.name}: ${e.message}`);
    }
}

tryCommand("prepare", () => bookstore.runCommand({ collMod: "reviews", prepareConstraintValidationLevel: true }));
tryCommand("constraint (1)", () => bookstore.runCommand({ collMod: "reviews", validationLevel: "constraint", validationAction: "error" }));

// Fix the old document that breaks the rule (string -> int)
const fixed = bookstore.reviews.updateMany({ rating: { $type: "string" } }, [{ $set: { rating: { $toInt: "$rating" } } }]);
print("fixed:", fixed.modifiedCount);
tryCommand("constraint (2)", () => bookstore.runCommand({ collMod: "reviews", validationLevel: "constraint", validationAction: "error" }));

tryCommand("bypassDocumentValidation", () => bookstore.runCommand({
    insert: "reviews", bypassDocumentValidation: true,
    documents: [{ bookId: 26, customer: { customerId: 38, name: "Margaret Hamilton" }, rating: 6, reviewedAt: new Date() }]
}));
tryCommand("change validator", () => bookstore.runCommand({ collMod: "reviews", validator: { $jsonSchema: { bsonType: "object" } } }));
const options = bookstore.getCollectionInfos({ name: "reviews" })[0].options;
print("level:", options.validationLevel, " action:", options.validationAction);
