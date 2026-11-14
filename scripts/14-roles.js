// 14 - least privilege: the actions behind readWrite and a custom role for the running application (admin)
const bookstore = db.getSiblingDB("bookstore");

const readWrite = db.runCommand({ rolesInfo: { role: "readWrite", db: "bookstore" }, showPrivileges: true, showBuiltinRoles: true });
const actions = readWrite.roles[0].privileges.find(p => p.resource.collection === "").actions;
print(`readWrite: ${actions.length} actions on every collection, among them:`,
      actions.filter(a => /^(drop|create|rename|convert)/.test(a)).sort().join(", "));

// The running API needs documents, not schema changes: no drop, no index or collection management
bookstore.createRole({
    role: "bookstoreApi",
    privileges: [
        { resource: { db: "bookstore", collection: "books" }, actions: ["find", "update"] },
        { resource: { db: "bookstore", collection: "orders" }, actions: ["find", "insert", "update"] },
        { resource: { db: "bookstore", collection: "reviews" }, actions: ["find", "insert", "update", "remove"] },
        { resource: { db: "bookstore", collection: "carts" }, actions: ["find", "insert", "update", "remove"] },
        { resource: { db: "bookstore", collection: "customers" }, actions: ["find", "insert", "update"] },
        { resource: { db: "bookstore", collection: "categories" }, actions: ["find"] },
        { resource: { db: "bookstore", collection: "authors" }, actions: ["find"] }
    ],
    roles: []
});
bookstore.createUser({ user: "bookstore_api", pwd: process.env.BOOKSTORE_API_PASSWORD || passwordPrompt(), roles: ["bookstoreApi"] });

// Try the role as bookstore_api
const api = new Mongo(`mongodb://bookstore_api:${encodeURIComponent(process.env.BOOKSTORE_API_PASSWORD)}@localhost:27017/bookstore?authSource=bookstore`)
    .getDB("bookstore");
function attempt(label, action) {
    try {
        print(label.padEnd(36), JSON.stringify(action()));
    } catch (e) {
        print(label.padEnd(36), `${e.codeName}: ${e.message.split(" on ")[0]}`);
    }
}
attempt("find a book:", () => api.books.findOne({ _id: 26 }, { title: 1, _id: 0 }));
attempt("decrement the stock:", () => api.books.updateOne({ _id: 26 }, { $inc: { "stock.onHand": -1 } }).modifiedCount);
attempt("delete a book:", () => api.books.deleteOne({ _id: 26 }).deletedCount);
attempt("drop orders:", () => api.orders.drop());
attempt("create an index:", () => api.orders.createIndex({ total: 1 }));
attempt("read another database:", () => api.getSiblingDB("admin").system.users.findOne());

// Undo
bookstore.books.updateOne({ _id: 26 }, { $inc: { "stock.onHand": 1 } });
bookstore.dropUser("bookstore_api");
bookstore.dropRole("bookstoreApi");
