export function featureMatrix() {
  return {
    localStorage: "localStorage" in window,
    indexedDB: "indexedDB" in window,
    caches: "caches" in window,
    opfs: typeof navigator.storage?.getDirectory === "function",
    createWritable: typeof FileSystemFileHandle !== "undefined" && "createWritable" in FileSystemFileHandle.prototype,
    cookieStore: "cookieStore" in window,
    storageBuckets: "storageBuckets" in navigator,
  };
}
