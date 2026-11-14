// How many characters fit into one localStorage value?
export function localStorageLimit() {
  let low = 0;
  let high = 20 * 1024 * 1024;
  let lastError = null;
  while (low < high) {
    const size = Math.ceil((low + high) / 2);
    try {
      localStorage.setItem("probe", "x".repeat(size));
      low = size;
    } catch (err) {
      lastError = err;
      high = size - 1;
    }
  }
  localStorage.removeItem("probe");
  console.log(`largest value: ${low} chars; error: ${lastError?.name}`);
}

// A reading list of 20,000 entries written to and read from localStorage on the main thread
export function readingListInLocalStorage() {
  const list = Array.from({ length: 20_000 }, (_, i) => ({
    isbn: String(9780000000000 + i),
    title: `Book ${i}`,
    addedAt: new Date(Date.UTC(2026, 10, 1, 0, 0, i)).toISOString(),
    note: "Read after Dune",
  }));
  let started = performance.now();
  const json = JSON.stringify(list);
  localStorage.setItem("readingList", json);
  const writeMs = performance.now() - started;

  started = performance.now();
  const back = JSON.parse(localStorage.getItem("readingList"));
  const readMs = performance.now() - started;
  console.log(`${json.length} chars: write ${writeMs.toFixed(1)} ms, read ${readMs.toFixed(1)} ms, ${back.length} items`);
  localStorage.removeItem("readingList");
}
