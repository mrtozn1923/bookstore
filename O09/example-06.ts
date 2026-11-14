type Customer = { id: number; name: string };

function assertIsDefined<T>(value: T, label: string): asserts value is NonNullable<T> {
  if (value === null || value === undefined) {
    throw new Error(`${label} is missing`);
  }
}

function assert(condition: unknown, message: string): asserts condition {
  if (!condition) {
    throw new Error(message);
  }
}

const customers = new Map<number, Customer>([[1, { id: 1, name: "Ada" }]]);

const ada = customers.get(1); // Customer | undefined
assertIsDefined(ada, "customer 1");
console.log(ada.name); // Ada

const quantity: unknown = 2;
assert(typeof quantity === "number", "quantity must be a number");
console.log(quantity * 450); // 900

const ghost = customers.get(99);
assertIsDefined(ghost, "customer 99"); // Error: customer 99 is missing
