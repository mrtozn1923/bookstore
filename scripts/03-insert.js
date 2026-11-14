// 03 - insertMany: ordered and unordered
db.books.deleteMany({ _id: { $in: [101, 102, 103] } });
const batch = [
    { _id: 101, isbn: "9798900001012", title: "Persuasion", listPrice: Decimal128("310.00") },
    { _id: 26, isbn: "9798900000268", title: "Dune", listPrice: Decimal128("390.50") },
    { _id: 102, isbn: "9798900001029", title: "Mansfield Park", listPrice: Decimal128("295.00") }
];

for (const ordered of [true, false]) {
    db.books.deleteMany({ _id: { $in: [101, 102] } });
    try {
        db.books.insertMany(batch, { ordered });
    } catch (e) {
        print(`ordered=${ordered}: ${e.name}, inserted=${e.result.insertedCount}, ` +
              `errors=${e.writeErrors.length} (index ${e.writeErrors[0].index}, code ${e.writeErrors[0].code})`);
    }
    print("  101/102 in collection:", db.books.countDocuments({ _id: { $in: [101, 102] } }));
}
db.books.deleteMany({ _id: { $in: [101, 102] } });
