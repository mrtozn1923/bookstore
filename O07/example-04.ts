type Customer = { id: number; name: string; email: string };

function applyChanges(customer: Customer, changes: Partial<Customer>): Customer {
  return { ...customer, ...changes };
}

const ada: Customer = { id: 1, name: "Ada", email: "ada@example.com" };
console.log(applyChanges(ada, { email: "ada.lovelace@example.com" }));
// { id: 1, name: 'Ada', email: 'ada.lovelace@example.com' }
