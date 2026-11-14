// 02 - multi-document transaction: needs a replica set
const session = db.getMongo().startSession();
const orders = session.getDatabase("bookstore").getCollection("tx_demo");
try {
    session.startTransaction();
    orders.insertOne({ _id: 1, note: "inside a transaction" });
    session.commitTransaction();
    print("committed, documents:", db.getSiblingDB("bookstore").tx_demo.countDocuments());
} catch (e) {
    print(e.name + ":", e.message);
} finally {
    db.getSiblingDB("bookstore").tx_demo.drop();
    session.endSession();
}
