import { assertNever } from "../src/order-status.ts";

type OrderStatusV2 =
  | { kind: "pending" }
  | { kind: "paid"; paymentId: string }
  | { kind: "refunded"; paymentId: string; amount: number };

function label(status: OrderStatusV2): string {
  switch (status.kind) {
    case "pending":
      return "pending";
    case "paid":
      return "paid";
    default:
      return assertNever(status);
  }
}

const impossible: OrderStatusV2 = { kind: "paid", paymentId: "PAY-1001", trackingNo: "TRK-778" };
