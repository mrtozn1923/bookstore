type Customer = {
  name: string;
  email: string | null;
  phone?: string;
};

type Email = NonNullable<Customer["email"]>; // string
type Phone = NonNullable<Customer["phone"]>; // string

function sendWelcome(email: Email) {
  console.log(`Welcome mail sent to ${email}`);
}

const customer: Customer = { name: "Grace", email: "grace@example.com" };

if (customer.email !== null) {
  sendWelcome(customer.email); // Welcome mail sent to grace@example.com
}
