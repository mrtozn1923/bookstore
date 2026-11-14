interface Entity {
  id: string;
  createdAt: Date;
}

interface Customer extends Entity {
  name: string;
  email: string;
}

const customer: Customer = {
  id: "customer_3",
  createdAt: new Date(),
  name: "Grace",
  email: "grace@example.com",
};
