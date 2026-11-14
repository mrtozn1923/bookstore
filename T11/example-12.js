const double = (n) => { n * 2 };
console.log(double(5)); // undefined

const makeCustomer = (name) => ({ name: name, active: true });
console.log(makeCustomer("Ada")); // { name: "Ada", active: true }
