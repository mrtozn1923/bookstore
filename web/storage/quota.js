const mb = (bytes) => (bytes / 1024 / 1024).toFixed(2);

export async function quota() {
  const { usage, quota, usageDetails } = await navigator.storage.estimate();
  const persisted = await navigator.storage.persisted();
  console.log(`usage ${mb(usage)} MB of ${(quota / 1024 ** 3).toFixed(1)} GB, persisted: ${persisted}`);
  if (usageDetails) console.log("usageDetails:", JSON.stringify(usageDetails));
}

// Ask only after a user action that shows the data matters (e.g. "keep my offline library")
export async function requestPersistence() {
  const granted = await navigator.storage.persist();
  console.log(`persist() -> ${granted}`);
  return granted;
}
