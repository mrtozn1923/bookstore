// The erasable alternative: a const object plus a union type
export const OrderStatus = {
    Pending: "Pending",
    Paid: "Paid",
    Shipped: "Shipped",
} as const;
export type OrderStatus = (typeof OrderStatus)[keyof typeof OrderStatus];

const status: OrderStatus = OrderStatus.Paid;
console.log(status);
