type PublicCustomer = { name: string };

function toJson(customer: PublicCustomer) {
  return JSON.stringify(customer);
}

const account = { name: "Ada", password: "secret123" };
console.log(toJson(account)); // {"name":"Ada","password":"secret123"}
