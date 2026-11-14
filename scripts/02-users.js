// 02 - application user: read/write on the bookstore database only
const bookstore = db.getSiblingDB("bookstore");
bookstore.createUser({
    user: "bookstore_app",
    pwd: process.env.BOOKSTORE_APP_PASSWORD || passwordPrompt(),
    roles: [{ role: "readWrite", db: "bookstore" }]
});
printjson(bookstore.getUser("bookstore_app", { showPrivileges: false }));
