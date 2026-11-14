let nextId = 1;

function createCustomer(name: string, city: string) {
  return { id: nextId++, name, city, createdAt: new Date() };
}

type Customer = ReturnType<typeof createCustomer>;
// { id: number; name: string; city: string; createdAt: Date; }

type CreateCustomerArgs = Parameters<typeof createCustomer>;
// [name: string, city: string]

type CustomerName = Parameters<typeof createCustomer>[0]; // string

function createMany(rows: CreateCustomerArgs[]): Customer[] {
  return rows.map((args) => createCustomer(...args));
}

console.log(createMany([["Ada", "Izmir"], ["Alan", "Ankara"]]).map((c) => c.id)); // [ 1, 2 ]

// Error: 'createCustomer' refers to a value, but is being used as a type here. Did you mean 'typeof createCustomer'?
type Wrong = ReturnType<createCustomer>;
