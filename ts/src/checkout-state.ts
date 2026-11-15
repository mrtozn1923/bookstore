import { assertNever } from "./order-status.ts";

export type CheckoutState =
  | { readonly kind: "idle" }
  | { readonly kind: "invalid"; readonly fieldErrors: Readonly<Record<string, readonly string[]>> }
  | { readonly kind: "rejected"; readonly message: string }
  | { readonly kind: "placed"; readonly orderNo: string; readonly total: number };

type ProblemDetails = { readonly title?: string; readonly errors?: Record<string, string[]> };
type OrderResponse = { readonly orderNo: string; readonly total: number };

// Turns an HTTP response into one UI state; the template never sees raw status codes
export function toCheckoutState(status: number, body: unknown): CheckoutState {
  if (status === 201) {
    const order = body as OrderResponse;
    return { kind: "placed", orderNo: order.orderNo, total: order.total };
  }
  const problem = body as ProblemDetails;
  if (status === 400 && problem.errors) {
    return { kind: "invalid", fieldErrors: problem.errors };
  }
  return { kind: "rejected", message: problem.title ?? `Unexpected status ${status}` };
}

export function banner(state: CheckoutState): string {
  switch (state.kind) {
    case "idle":
      return "";
    case "invalid":
      return `Please fix ${Object.keys(state.fieldErrors).length} field(s): ${Object.keys(state.fieldErrors).join(", ")}`;
    case "rejected":
      return `Order not placed: ${state.message}`;
    case "placed":
      return `Thank you! Order ${state.orderNo}, total ${state.total.toFixed(2)} TL`;
    default:
      return assertNever(state);
  }
}
