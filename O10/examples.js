// O10: run with `node O10/examples.js` (or paste the blocks into the browser console in order)

// --- example 1 ---
async function run() {
  console.log("2");
  await null;
  console.log("4");
}

console.log("1");
run();
console.log("3");

// --- example 2 ---
async function getNumber() {
  return 42;
}

const result = getNumber();
console.log(result instanceof Promise); // true
result.then((value) => console.log(value)); // 42

// --- example 3 ---
async function fail() {
  throw new Error("Something went wrong");
}

fail().catch((error) => console.error(error.message));

// --- example 4 ---
function wait(ms) {
  return new Promise((resolve) => setTimeout(() => resolve(`${ms} ms passed`), ms));
}

async function demo() {
  console.log("Started");
  const message = await wait(1000);
  console.log(message); // "1000 ms passed"
  console.log("Done");
}

demo();

// --- example 5 ---
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

async function showFirstOrder(customerId) {
  const customer = await getCustomer(customerId);
  const orders = await getOrders(customer.id);
  const detail = await getOrderDetail(orders[0].id);
  console.log(`${customer.name} paid ${detail.total} TRY on their first order`);
}

showFirstOrder(1);

// --- example 6 ---
async function showCustomer(id) {
  console.log("Loading...");
  try {
    const customer = await getCustomer(id);
    console.log(`Welcome, ${customer.name}`);
  } catch (error) {
    console.error(`Error: ${error.message}`);
  } finally {
    console.log("Loading finished");
  }
}

showCustomer(1);  // Loading... → Welcome, Ada → Loading finished
showCustomer(-1); // Loading... → Error: Invalid customer id → Loading finished

// --- example 7 ---
async function sequential() {
  console.time("sequential");
  const a = await wait(1000);
  const b = await wait(1000);
  const c = await wait(1000);
  console.timeEnd("sequential"); // about 3000 ms
}

async function parallel() {
  console.time("parallel");
  const [a, b, c] = await Promise.all([wait(1000), wait(1000), wait(1000)]);
  console.timeEnd("parallel"); // about 1000 ms
}

sequential().then(parallel);

// --- example 8 ---
async function loadAll() {
  const ids = [1, 2, 3];
  ids.forEach(async (id) => {
    const customer = await getCustomer(id);
    console.log(customer.id);
  });
  console.log("All loaded"); // this prints first!
}

loadAll();

// --- example 9 ---
async function loadSequential(ids) {
  for (const id of ids) {
    const customer = await getCustomer(id);
    console.log("sequential:", customer.id);
  }
  console.log("Sequential load finished");
}

async function loadParallel(ids) {
  const customers = await Promise.all(ids.map((id) => getCustomer(id)));
  console.log("parallel:", customers.map((customer) => customer.id));
}

loadSequential([1, 2, 3]).then(() => loadParallel([1, 2, 3]));

// --- example 11 ---
async function broken() {
  const customer = getCustomer(1); // no await
  console.log(customer.name);      // undefined, because customer is a Promise
}

broken();

// --- example 12 ---
async function a() {
  console.log("a1");
  await wait(0);
  console.log("a2");
}

async function b() {
  console.log("b1");
  await null;
  console.log("b2");
}

a();
b();
console.log("end");
