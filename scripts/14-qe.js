// 14 - Queryable Encryption with explicit encryption (Community): the phone number is encrypted in the client (admin)
const localKey = crypto.randomBytes(96);   // lab only: a real deployment keeps the master key in a KMS
const encrypted = Mongo(db.getMongo()._uri, {
    keyVaultNamespace: "encryption.__keyVault",
    kmsProviders: { local: { key: localKey } },
    bypassQueryAnalysis: true
});
const clientEncryption = encrypted.getClientEncryption();
const bookstore = db.getSiblingDB("bookstore");

bookstore.customers_secure.drop();
clientEncryption.createEncryptedCollection("bookstore", "customers_secure", {
    provider: "local",
    createCollectionOptions: { encryptedFields: { fields: [{ path: "phone", bsonType: "string", queries: { queryType: "equality", contention: 0 } }] } }
});
const keyId = bookstore.getCollectionInfos({ name: "customers_secure" })[0].options.encryptedFields.fields[0].keyId;

// Explicit encryption: the application encrypts the value itself before it leaves the process
const secure = encrypted.getDB("bookstore").customers_secure;
const encryptPhone = phone => clientEncryption.encrypt(keyId, phone, { algorithm: "Indexed", contentionFactor: 0 });
bookstore.customers.find({ phone: { $exists: true } }).sort({ _id: 1 }).limit(3).forEach(c =>
    secure.insertOne({ _id: c._id, name: `${c.firstName} ${c.lastName}`, phone: encryptPhone(c.phone) }));

// Equality query on the encrypted field: the query value is encrypted too
const target = bookstore.customers.findOne({ _id: 4 }).phone;
const query = clientEncryption.encrypt(keyId, target, { algorithm: "Indexed", contentionFactor: 0, queryType: "equality" });
const found = secure.findOne({ phone: query });
print("found through the encrypted client:", found.name, found.phone);

// The same document through a normal connection (what a database admin or a backup sees)
const raw = bookstore.customers_secure.findOne({ _id: 4 });
print("raw phone field:", raw.phone.constructor.name, "subtype", raw.phone.sub_type, "|", raw.phone.length(), "bytes");
print("other fields in the raw document:", Object.keys(raw).join(", "));
print("plain-text search for the phone:", bookstore.customers_secure.countDocuments({ phone: target }), "documents");

// Undo: the lab key is thrown away, so the data could not be read again anyway
bookstore.customers_secure.drop();
db.getSiblingDB("encryption").dropDatabase();
