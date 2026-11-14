function sendWelcome(customer: { name: string; email?: string }) {
  console.log(customer.name);
}

// Error: Object literal may only specify known properties, but 'emial' does not exist in type '{ name: string; email?: string | undefined; }'. Did you mean to write 'email'?
sendWelcome({ name: "Grace", emial: "grace@example.com" });
