type Email = string & { readonly __brand: "Email" };

function isEmail(value: string): value is Email {
  return value.includes("@");
}

function sendWelcome(to: Email) {
  console.log(`Welcome mail sent to ${to}`);
}

const input = "ada@example.com";

if (isEmail(input)) {
  sendWelcome(input); // Welcome mail sent to ada@example.com
}
