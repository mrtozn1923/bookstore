type Customer = { id: number; name: string };

// Error: Property 'phone' does not exist on type 'Customer'.
type Phone = Customer["phone"];

// Error: Cannot access 'Customer.name' because 'Customer' is a type, but not a namespace. Did you mean to retrieve the type of the property 'name' in 'Customer' with 'Customer["name"]'?
type Name = Customer.name;

const key = "name";
// Error: Type 'key' cannot be used as an index type.
// Error: 'key' refers to a value, but is being used as a type here. Did you mean 'typeof key'?
type ByKey = Customer[key];

type ByKeyFixed = Customer[typeof key]; // string
