// 01 - a quick look at the server: version, storage engine, cache, topology
const status = db.serverStatus();
print("version       :", db.version());
print("storageEngine :", status.storageEngine.name);
print("cache (bytes) :", status.wiredTiger.cache["maximum bytes configured"]);
print("replica set   :", db.hello().setName ?? "(none, standalone)");
