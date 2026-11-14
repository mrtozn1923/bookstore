import { openBookstoreDb } from "./open-db.js";

export { openBookstoreDb };

export async function upgradeDemo(version) {
  const db = await openBookstoreDb(version);
  const stores = [...db.objectStoreNames].map((name) => {
    const indexes = [...db.transaction(name).objectStore(name).indexNames];
    return indexes.length ? `${name} [${indexes.join(", ")}]` : name;
  });
  console.log(`version ${db.version}: ${stores.join("; ")}`);
  db.close();
}

const dune = { isbn: "9780441172719", title: "Dune", addedAt: "2026-11-14T09:00:00Z" };
const emma = { isbn: "9780141439587", title: "Emma", addedAt: "2026-11-14T09:05:00Z" };

// A transaction commits as soon as it has no pending requests and control returns to the event loop
export async function autoCommitDemo() {
  const db = await openBookstoreDb(2);
  const tx = db.transaction("readingList", "readwrite");
  const store = tx.objectStore("readingList");
  store.put(dune);
  tx.oncomplete = () => console.log("transaction complete");

  const price = await (await fetch("/api/books/9780141439587")).json(); // not an IndexedDB request
  try {
    store.put({ ...emma, price: price.price });
  } catch (err) {
    console.log(`${err.name}: ${err.message}`);
  }
  db.close();
}

// The fix: do the network work first, then write everything in one short transaction
export async function autoCommitFixed() {
  const book = await (await fetch("/api/books/9780141439587")).json();
  const db = await openBookstoreDb(2);
  const tx = db.transaction("readingList", "readwrite");
  tx.objectStore("readingList").put(dune);
  tx.objectStore("readingList").put({ ...emma, price: book.price });
  await new Promise((resolve, reject) => {
    tx.oncomplete = resolve;
    tx.onerror = () => reject(tx.error);
  });
  const count = await new Promise((resolve) => {
    const request = db.transaction("readingList").objectStore("readingList").count();
    request.onsuccess = () => resolve(request.result);
  });
  console.log(`saved, reading list has ${count} books`);
  db.close();
}

// tab A keeps an old connection open; the handler decides whether it gives way
export async function holdConnection(version, closeOnVersionChange) {
  window.heldDb = await openBookstoreDb(version, {
    onVersionChange: closeOnVersionChange ? (db) => db.close() : undefined,
  });
  return `holding version ${window.heldDb.version}`;
}

export async function deleteDatabase() {
  await new Promise((resolve) => {
    const request = indexedDB.deleteDatabase("bookstore");
    request.onsuccess = resolve;
    request.onblocked = () => console.log("delete blocked");
  });
  return "deleted";
}
