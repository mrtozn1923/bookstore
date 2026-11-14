function sendWelcome(customer: { name: string; email?: string }) {
  // Error: 'customer.email' is possibly 'undefined'.
  console.log(customer.email.toLowerCase());

  if (customer.email !== undefined) {
    console.log(`Sending to ${customer.email.toLowerCase()}`);
  }
}

sendWelcome({ name: "Ada" });
sendWelcome({ name: "Grace", email: "Grace@Example.com" });
// Sending to grace@example.com
