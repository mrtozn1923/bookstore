export type OrderStatus =
  | { readonly kind: "pending" }
  | { readonly kind: "paid"; readonly paymentId: string }
  | { readonly kind: "shipped"; readonly paymentId: string; readonly trackingNo: string }
  | { readonly kind: "cancelled"; readonly reason: string };

export function assertNever(value: never): never {
  throw new Error(`Unexpected value: ${JSON.stringify(value)}`);
}

export function describe(status: OrderStatus): string {
  switch (status.kind) {
    case "pending":
      return "pending";
    case "paid":
      return `paid with ${status.paymentId}`;
    case "shipped":
      return `shipped with ${status.trackingNo}`;
    case "cancelled":
      return `cancelled: ${status.reason}`;
    default:
      return assertNever(status);
  }
}

// Only orders that haven't shipped can be cancelled
export type Cancellable = Extract<OrderStatus, { kind: "pending" | "paid" }>;

export function cancel(status: Cancellable, reason: string): OrderStatus {
  return { kind: "cancelled", reason };
}
