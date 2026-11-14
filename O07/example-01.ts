type Customer = { id: number; name: string; email: string };

function updateCustomer(id: number, changes: Partial<Customer>) {
  console.log(id, changes);
}

updateCustomer(1, { name: "Grace" });
updateCustomer(1, {});
updateCustomer(1, { phone: "555 0100" });
