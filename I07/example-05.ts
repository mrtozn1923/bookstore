type CustomerId = string & { readonly __brand: "CustomerId" };

function toCustomerId(raw: string): CustomerId {
  if (!/^customer_\d+$/.test(raw)) {
    throw new Error(`Invalid customer id: ${raw}`);
  }
  return raw as CustomerId;
}

const id = toCustomerId("customer_1");
console.log(id.toUpperCase()); // CUSTOMER_1
console.log(typeof id); // string
console.log(JSON.stringify({ id })); // {"id":"customer_1"}
