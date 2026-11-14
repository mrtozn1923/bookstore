type CustomerId = string & { readonly __brand: "CustomerId" };
type OrderId = string & { readonly __brand: "OrderId" };

function cancelOrder(id: OrderId) {
  console.log(`Cancelling order ${id}`);
}

const customerId = "customer_1" as CustomerId;

cancelOrder(customerId); // Error
cancelOrder("order_1042"); // Error
