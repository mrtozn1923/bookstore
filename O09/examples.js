// O09: run with `node O09/examples.js` (or paste the blocks into the browser console in order)

// --- example 1 ---
const p = new Promise((resolve) => {
  console.log("1");
  resolve("2");
  console.log("3");
});

p.then((value) => console.log(value));
console.log("4");

// --- example 2 ---
function wait(ms) {
  return new Promise((resolve) => {
    setTimeout(() => resolve(`${ms} ms passed`), ms);
  });
}

wait(1000).then((message) => console.log(message)); // after 1 second: "1000 ms passed"

// --- example 3 ---
function checkStock() {
  return new Promise((resolve, reject) => {
    setTimeout(() => {
      const copies = Math.floor(Math.random() * 6); // 0–5 copies
      if (copies === 0) {
        reject(new Error("Dune is out of stock"));
      } else {
        resolve(copies);
      }
    }, 500);
  });
}

checkStock()
  .then((copies) => console.log(`Dune: ${copies} in stock`))
  .catch((error) => console.error(error.message));

// --- example 4 ---
console.log("Loading...");

checkStock()
  .then((copies) => console.log(`Dune: ${copies} in stock`))
  .catch((error) => console.error(error.message))
  .finally(() => console.log("Loading finished"));

// --- example 5 ---
wait(500)
  .then(() => 2)
  .then((n) => n * 10)
  .then((n) => wait(500).then(() => n + 1))
  .then((n) => console.log(n)); // 21 after about 1 second

// --- example 6 ---
function getCustomer(id) {
  return new Promise((resolve, reject) => {
    setTimeout(() => {
      if (id <= 0) reject(new Error("Invalid customer id"));
      else resolve({ id, name: "Ada" });
    }, 500);
  });
}

function getOrders(customerId) {
  return wait(500).then(() => [{ id: 1041 }, { id: 1042 }]);
}

function getOrderDetail(orderId) {
  return wait(500).then(() => ({ id: orderId, total: 450 }));
}

getCustomer(1)
  .then((customer) => getOrders(customer.id))
  .then((orders) => getOrderDetail(orders[0].id))
  .then((detail) => console.log(`First order: ${detail.total} TRY`))
  .catch((error) => console.error(error.message));

// --- example 7 ---
wait(300)
  .then(() => {
    throw new Error("Something went wrong in step two");
  })
  .then(() => console.log("This line never runs"))
  .catch((error) => {
    console.error("Caught:", error.message);
    return "default value";
  })
  .then((value) => console.log("The chain carries on:", value));

// --- example 8 ---
Promise.all([wait(1000), wait(500), wait(200)])
  .then((results) => console.log(results));
// after about 1 second: ["1000 ms passed", "500 ms passed", "200 ms passed"]

// --- example 9 ---
function timeout(ms) {
  return new Promise((_, reject) => {
    setTimeout(() => reject(new Error("Timed out")), ms);
  });
}

Promise.race([wait(2000), timeout(1000)])
  .then((message) => console.log(message))
  .catch((error) => console.error(error.message)); // "Timed out"

// --- example 10 ---
// Old callback-based API
function loadPreferences(callback) {
  setTimeout(() => callback(null, { currency: "TRY" }), 300);
}

// Promise wrapper
function loadPreferencesAsync() {
  return new Promise((resolve, reject) => {
    loadPreferences((error, preferences) => {
      if (error) reject(error);
      else resolve(preferences);
    });
  });
}

loadPreferencesAsync().then((preferences) => console.log(preferences.currency)); // "TRY"

// --- example 11 ---
getCustomer(1)
  .then((customer) => {
    getOrders(customer.id); // no return!
  })
  .then((orders) => console.log(orders)); // undefined

// --- example 12 ---
setTimeout(() => console.log("A"), 0);

Promise.resolve("B")
  .then((value) => {
    console.log(value);
    throw new Error("C");
  })
  .catch((error) => console.log(error.message))
  .finally(() => console.log("D"));

console.log("E");
