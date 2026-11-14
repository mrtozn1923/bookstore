// O15: examples that need a browser; paste these blocks into the DevTools console one by one
// (if a block uses a function from examples.js, paste that file first)

// --- example 1 ---
localStorage.setItem("cartCount", 5);
const cartCount = localStorage.getItem("cartCount");
console.log(cartCount + 1);

localStorage.setItem("customer", { name: "Ada" });
console.log(localStorage.getItem("customer"));

localStorage.removeItem("cartCount");
localStorage.removeItem("customer");

// --- example 2 ---
localStorage.setItem("theme", "dark");   // write
localStorage.getItem("theme");           // "dark"
localStorage.getItem("missing");         // null
localStorage.length;                     // number of entries
localStorage.key(0);                     // name of the key at index 0
localStorage.removeItem("theme");        // delete one entry
localStorage.clear();                    // delete every entry for this site

// --- example 3 ---
function saveJSON(key, value) {
  localStorage.setItem(key, JSON.stringify(value));
}

function loadJSON(key, fallback) {
  const raw = localStorage.getItem(key);
  if (raw === null) return fallback;
  try {
    return JSON.parse(raw);
  } catch {
    return fallback;
  }
}

saveJSON("cart", [{ isbn: "9780441172719", quantity: 2 }]);
console.log(loadJSON("cart", []));        // [{ isbn: "9780441172719", quantity: 2 }]
console.log(loadJSON("nothing", []));     // []

// --- example 7 ---
document.cookie = "currency=TRY; path=/; max-age=2592000; SameSite=Lax; Secure";
document.cookie = "banner=closed; path=/; max-age=86400; SameSite=Lax; Secure";

console.log(document.cookie); // "currency=TRY; banner=closed" (attributes aren't shown)

function getCookie(name) {
  const pair = document.cookie
    .split("; ")
    .find((item) => item.startsWith(`${name}=`));
  return pair ? decodeURIComponent(pair.slice(name.length + 1)) : null;
}

console.log(getCookie("currency")); // "TRY"

// Deleting: reset its lifetime with the same path
document.cookie = "banner=; path=/; max-age=0";
