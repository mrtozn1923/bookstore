const customer = { name: "Ada", address: { city: "Izmir" } };
const deep = structuredClone(customer);

deep.address.city = "Ankara";
console.log(customer.address.city); // "Izmir"
console.log(deep.address.city);     // "Ankara"
