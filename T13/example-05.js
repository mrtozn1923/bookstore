const account = { name: "Ada", email: "ada@example.com", password: "secret123" };

console.log(JSON.stringify(account, ["name", "email"]));
// {"name":"Ada","email":"ada@example.com"}

const safe = JSON.stringify(account, (key, value) => {
  if (key === "password") {
    return undefined;
  }
  return value;
});
console.log(safe); // {"name":"Ada","email":"ada@example.com"}
