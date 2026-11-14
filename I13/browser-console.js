// I13: examples that need a browser; paste these blocks into the DevTools console one by one
// (if a block uses a function from examples.js, paste that file first)

// --- example 4 ---
const reader = new FileReader();
reader.addEventListener("load", () => console.log(reader.result));
reader.addEventListener("error", () => console.error(reader.error));
reader.readAsText(file);

// --- example 8 ---
const titleBytes = new TextEncoder().encode("Dune");
console.log(titleBytes.toBase64()); // "RHVuZQ=="
console.log(titleBytes.toHex());    // "44756e65"

const decoded = Uint8Array.fromBase64("RHVuZQ==");
console.log(new TextDecoder().decode(decoded)); // "Dune"

// --- example 9 ---
function downloadFile(content, fileName, type) {
  const blob = new Blob([content], { type });
  const url = URL.createObjectURL(blob);

  const link = document.createElement("a");
  link.href = url;
  link.download = fileName;
  link.click();

  setTimeout(() => URL.revokeObjectURL(url), 1000); // release after the download starts
}

const order = { id: 1042, customer: "Ada", items: ["Dune", "Emma"], total: 770, currency: "TRY" };
downloadFile(JSON.stringify(order, null, 2), "order-1042.json", "application/json");
