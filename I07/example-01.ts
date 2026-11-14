type CustomerId = string;
type OrderId = string;

function cancelOrder(id: OrderId) {
  console.log(`Cancelling order ${id}`);
}

const customerId: CustomerId = "customer_1";
cancelOrder(customerId);
