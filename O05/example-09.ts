const statusLabels = {
  pending: "Pending",
  paid: "Paid",
  shipped: "Shipped",
} as const;

type Status = keyof typeof statusLabels; // "paid" | "pending" | "shipped"

function getLabel(status: Status): string {
  return statusLabels[status];
}

console.log(getLabel("shipped")); // Shipped
// Error: Argument of type '"lost"' is not assignable to parameter of type '"paid" | "pending" | "shipped"'.
getLabel("lost");
