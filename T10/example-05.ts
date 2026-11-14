type Validator = {
  (value: string): boolean;
  description: string;
};

function isEmail(value: string) {
  return value.includes("@");
}
isEmail.description = "Contains an @ sign";

const check: Validator = isEmail;
console.log(check.description, check("ada@example.com")); // Contains an @ sign true
