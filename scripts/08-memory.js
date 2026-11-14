// 08 - MongoDB 9.0: per-operation memory limit (needs admin; the limit is lowered only for this test)
const bookstore = db.getSiblingDB("bookstore");
bookstore.memory_test.drop();
const payload = "x".repeat(1000);
for (let b = 0; b < 30; b++) {
    bookstore.memory_test.insertMany(Array.from({ length: 1000 }, (_, i) => ({ _id: b * 1000 + i, bookId: i % 77, payload })));
}
const pipeline = [{ $group: { _id: "$bookId", payloads: { $push: "$payload" } } }];

function run(label, options) {
    try {
        const n = bookstore.memory_test.aggregate(pipeline, options).toArray().length;
        print(`${label}: ok, ${n} groups`);
    } catch (err) {
        print(`${label}: ${err.codeName}`);
    }
}

const param = "internalQueryMaxMemoryUsageBytesPerOperation";
let original;
try {
    original = db.adminCommand({ getParameter: 1, [param]: 1 })[param];
} catch (err) {
    print(`${param}: ${err.message}`);
}
if (original !== undefined) {
    print("default limit:", Math.round(Number(original) / 1024 / 1024), "MB",
          " peak memory of the $group:", Number(bookstore.memory_test.explain("executionStats").aggregate(pipeline).peakTrackedMemBytes), "bytes");
    db.adminCommand({ setParameter: 1, [param]: 20 * 1024 * 1024 });
    run("20 MB limit, allowDiskUse: false", { allowDiskUse: false });
    run("20 MB limit, allowDiskUse: true ", { allowDiskUse: true });
    db.adminCommand({ setParameter: 1, [param]: original });
}
run("default,     allowDiskUse: false", { allowDiskUse: false });
bookstore.memory_test.drop();
