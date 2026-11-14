const customer = { name: "Ada" };

try {
  JSON.parse(customer);
} catch (error) {
  console.log(error.name); // "SyntaxError"
}
