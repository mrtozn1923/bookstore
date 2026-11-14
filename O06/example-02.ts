type Order = {
  id: string;
  status: "pending" | "paid" | "shipped";
  customer: { id: string; email: string };
};

function setStatus(order: Order, status: Order["status"]) {
  order.status = status;
}

function sendReceipt(email: Order["customer"]["email"]) {
  console.log(`Receipt sent to ${email}`);
}

sendReceipt("ada@example.com"); // Receipt sent to ada@example.com
