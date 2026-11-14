// 12 - write concern, read concern and read preference with lagging secondaries (admin: fsyncLock)
const user = process.env.MONGO_ROOT_USER;
const pwd = process.env.MONGO_ROOT_PASSWORD;
const direct = host => new Mongo(`mongodb://${user}:${pwd}@${host}/?authSource=admin&directConnection=true`);
const hello = db.hello();
const secondaries = hello.hosts.filter(h => h !== hello.primary).map(direct);
const orders = db.getSiblingDB("bookstore").orders;

function show(label, read) {
    const values = [379, 380].map(id => {
        const doc = read(id);
        return doc ? `${id} ${doc.status}` : `${id} not found`;
    });
    print(label.padEnd(34), values.join(", "));
}
function attempt(label, action) {
    try {
        print(label.padEnd(34), action());
    } catch (e) {
        print(label.padEnd(34), `${e.codeName || e.name}: ${e.message.split(" {")[0]}`);
    }
}

const rw = db.adminCommand({ getDefaultRWConcern: 1 });
print("default write concern:", JSON.stringify(rw.defaultWriteConcern), "source:", rw.defaultWriteConcernSource);
const limits = db.adminCommand({ getParameter: 1, transactionLifetimeLimitSeconds: 1, maxConcurrentMultiDocumentTransactions: 1 });
print("transaction lifetime:", limits.transactionLifetimeLimitSeconds, "s, max open transactions:", limits.maxConcurrentMultiDocumentTransactions);

// Both secondaries stop applying the oplog: replication lag we can see
secondaries.forEach(s => s.getDB("admin").fsyncLock());

// The payment service: order 379 becomes Paid, order 380 is created, both with w:1
orders.updateOne({ _id: 379 }, { $set: { status: "Paid" } }, { writeConcern: { w: 1 } });
orders.insertOne({ _id: 380, customer: { customerId: 3, name: "Grace Hopper" }, orderedAt: new Date(),
    status: "Paid", items: [], total: Decimal128("0"), currency: "TRY", payments: [] }, { writeConcern: { w: 1 } });

show("primary, read concern local:", id => orders.findOne({ _id: id }));
show("secondary:", id => orders.find({ _id: id }).readPref("secondary").next() ?? null);
show("primary, read concern majority:", id => orders.find({ _id: id }).readConcern("majority").next() ?? null);

// w:"majority" with a timeout: the error does not undo the write
attempt("w: majority, wtimeout 2000:", () => {
    const r = orders.updateOne({ _id: 378 }, { $set: { note: "checked" } }, { writeConcern: { w: "majority", wtimeout: 2000 } });
    return JSON.stringify(r);
});
print("order 378 note on the primary:".padEnd(34), orders.findOne({ _id: 378 }).note);

// Causal consistency: a read in the same session waits for the session's own write
const session = db.getMongo().startSession({ causalConsistency: true });
const sessionOrders = session.getDatabase("bookstore").orders;
sessionOrders.updateOne({ _id: 379 }, { $set: { status: "Shipped" } }, { writeConcern: { w: 1 } });
attempt("causal session, secondary read:", () => sessionOrders.find({ _id: 379 }).readPref("secondary").maxTimeMS(2000).next().status);
attempt("read concern linearizable:", () => orders.find({ _id: 379 }).readConcern("linearizable").maxTimeMS(2000).next().status);

secondaries.forEach(s => s.getDB("admin").fsyncUnlock());
sleep(1000);
print("--- secondaries caught up");
show("secondary:", id => orders.find({ _id: id }).readPref("secondary").next() ?? null);
show("primary, read concern majority:", id => orders.find({ _id: id }).readConcern("majority").next() ?? null);
attempt("causal session, secondary read:", () => sessionOrders.find({ _id: 379 }).readPref("secondary").maxTimeMS(2000).next().status);
session.endSession();

// Undo
orders.deleteOne({ _id: 380 });
orders.updateOne({ _id: 379 }, { $set: { status: "Pending" } });
orders.updateOne({ _id: 378 }, { $unset: { note: "" } });
