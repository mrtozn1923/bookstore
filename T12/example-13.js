const customer = { name: "Ada" };
console.log(customer.address);  // undefined

try {
  console.log(customer.address.city);
} catch (error) {
  console.log(error.name);  // "TypeError"
}
