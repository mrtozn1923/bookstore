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
