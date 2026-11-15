import { cancel, type OrderStatus } from "../src/order-status.ts";

const shipped: OrderStatus = { kind: "shipped", paymentId: "PAY-1001", trackingNo: "TRK-778" };
cancel(shipped, "too late");
