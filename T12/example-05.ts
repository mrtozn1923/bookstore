type Customer = { id: number; name: string };

const customers: Customer[] = [
  { id: 1, name: "Ada" },
  { id: 2, name: "Alan" },
];

const found = customers.find((customer) => customer.id === 3);

// Error: 'found' is possibly 'undefined'.
console.log(found.name);
