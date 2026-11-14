// One function per schema version; a user may jump from any older version to the newest
const migrations = {
  1: (db) => {
    db.createObjectStore("readingList", { keyPath: "isbn" });
  },
  2: (db, tx) => {
    tx.objectStore("readingList").createIndex("byAddedAt", "addedAt");
    db.createObjectStore("outbox", { keyPath: "idempotencyKey" }).createIndex("byCreatedAt", "createdAt");
  },
};

export function openBookstoreDb(version, { onVersionChange } = {}) {
  return new Promise((resolve, reject) => {
    const request = indexedDB.open("bookstore", version);
    request.onupgradeneeded = (event) => {
      console.log(`upgrade ${event.oldVersion} -> ${event.newVersion}`);
      for (let v = event.oldVersion + 1; v <= event.newVersion; v++) {
        migrations[v](request.result, request.transaction); // runs inside the versionchange transaction
      }
    };
    request.onblocked = (event) => console.log(`blocked: another tab still has version ${event.oldVersion} open`);
    request.onsuccess = () => {
      const db = request.result;
      db.onversionchange = (event) => {
        console.log(`versionchange: ${event.oldVersion} -> ${event.newVersion} requested elsewhere`);
        if (onVersionChange) onVersionChange(db);
      };
      resolve(db);
    };
    request.onerror = () => reject(request.error);
  });
}
