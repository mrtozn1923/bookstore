// O08: run with `node O08/examples.js` (or paste the blocks into the browser console in order)

// --- example 1 ---
console.log("A");
setTimeout(() => console.log("B"), 0);
console.log("C");

// --- example 3 ---
function multiply(a, b) {
  return a * b;
}

function square(n) {
  return multiply(n, n);
}

function printSquare(n) {
  const result = square(n);
  console.log(result);
}

printSquare(4); // 16

// --- example 4 ---
console.log("A");                       // 1) runs immediately: A
setTimeout(() => console.log("B"), 0);  // 2) handed to the browser, the time is up at once,
                                        //    the function enters the queue
console.log("C");                       // 3) runs immediately: C
// 4) the script is done, the stack is empty, the event loop checks the queue: B

// --- example 5 ---
const start = Date.now();

setTimeout(() => {
  console.log(`Elapsed: ${Date.now() - start} ms`);
}, 100);

while (Date.now() - start < 1000) {
  // keep the stack busy for 1 second
}

// --- example 6 ---
function getCustomer(id, callback) {
  setTimeout(() => {
    if (id <= 0) {
      callback(new Error("Invalid customer id"));
      return;
    }
    callback(null, { id, name: "Ada" });
  }, 500);
}

getCustomer(1, (error, customer) => {
  if (error) {
    console.error(error.message);
    return;
  }
  console.log(`Customer: ${customer.name}`);
});

console.log("Request sent");

// --- example 7 ---
function getOrders(customerId, callback) {
  setTimeout(() => callback(null, [{ id: 1041 }, { id: 1042 }]), 500);
}

function getOrderDetail(orderId, callback) {
  setTimeout(() => callback(null, { id: orderId, total: 450 }), 500);
}

getCustomer(1, (error, customer) => {
  if (error) return console.error(error.message);

  getOrders(customer.id, (error, orders) => {
    if (error) return console.error(error.message);

    getOrderDetail(orders[0].id, (error, detail) => {
      if (error) return console.error(error.message);

      console.log(`${customer.name} paid ${detail.total} TRY on their first order`);
    });
  });
});

// --- example 8 ---
function getCustomerName() {
  let name;
  setTimeout(() => {
    name = "Ada";
  }, 500);
  return name;
}

console.log(getCustomerName()); // undefined

// --- example 9 ---
console.log(1);
setTimeout(() => console.log(2), 100);
setTimeout(() => console.log(3), 0);
console.log(4);
