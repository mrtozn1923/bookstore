const customer = { name: "Ada", address: { city: "Izmir" } };
const copy = { ...customer };

copy.name = "Grace";
copy.address.city = "Ankara";

console.log(customer.name, customer.address.city);
