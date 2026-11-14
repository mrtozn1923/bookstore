type OrderStatus = "pending" | "paid" | "shipped";

let current: OrderStatus = "pending";
current = "paid";
current = "completed";

function formatId(id: string | number) {
  return id.toUpperCase();
}
