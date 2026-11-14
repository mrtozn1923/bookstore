// O06: run with `node O06/examples.js` (or paste the blocks into the browser console in order)

// --- example 1 ---
function test() {
  try {
    return "try";
  } finally {
    console.log("finally");
  }
}

console.log(test());

// --- example 2 ---
try {
  const data = JSON.parse("{ broken json");
  console.log(data); // never runs
} catch (error) {
  console.log(error.name);    // "SyntaxError"
  console.log(error.message); // wording differs between browsers
} finally {
  console.log("runs no matter what");
}

console.log("the program carries on");

// --- example 3 ---
function reserveStock(inStock, quantity) {
  if (typeof quantity !== "number" || Number.isNaN(quantity)) {
    throw new TypeError("Quantity must be a number");
  }
  if (quantity > inStock) {
    throw new RangeError(`Not enough stock: ${inStock} left, ${quantity} requested`);
  }
  return inStock - quantity;
}

try {
  reserveStock(3, 5);
} catch (error) {
  console.log(error.name);    // "RangeError"
  console.log(error.message); // "Not enough stock: 3 left, 5 requested"
  console.log(error.stack);   // where the error was created
}

// --- example 5 ---
class ValidationError extends Error {
  constructor(field, message) {
    super(message);
    this.name = "ValidationError";
    this.field = field;
  }
}

function validateEmail(email) {
  if (!email.includes("@")) {
    throw new ValidationError("email", "Please enter a valid email address");
  }
  return email;
}

try {
  validateEmail("ada.example.com");
} catch (error) {
  if (error instanceof ValidationError) {
    console.log(`${error.field}: ${error.message}`); // "email: Please enter a valid email address"
  } else {
    throw error;
  }
}

// --- example 6 ---
function loadCart(text) {
  try {
    return JSON.parse(text);
  } catch (error) {
    throw new Error("Could not load cart", { cause: error });
  }
}

try {
  loadCart("{ items: [Dune] }");
} catch (error) {
  console.log(error.message);       // "Could not load cart"
  console.log(error.cause.name);    // "SyntaxError"
  console.log(error.cause.message); // the original JSON.parse message
}

// --- example 7 ---
const fake = Object.create(Error.prototype);
console.log(fake instanceof Error);              // true
console.log(Error.isError(fake));                // false
console.log(Error.isError(new TypeError("x")));  // true
console.log(Error.isError({ message: "x" }));    // false

// --- example 8 ---
const isError = (value) =>
  typeof Error.isError === "function" ? Error.isError(value) : value instanceof Error;

console.log(isError(new RangeError("x"))); // true
