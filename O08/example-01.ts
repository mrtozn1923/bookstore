type OrderStatus = "pending" | "shipped" | "cancelled" | null;

type A = Exclude<OrderStatus, "cancelled">;
type B = NonNullable<A>;
type C = Extract<OrderStatus, "pending" | "lost">;
