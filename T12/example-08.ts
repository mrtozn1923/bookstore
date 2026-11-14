const checkout = {
  currency: "TRY",
  freeShippingOver: 500,
  paymentMethods: ["card", "transfer"],
} as const;

// Error: Cannot assign to 'currency' because it is a read-only property.
checkout.currency = "EUR";

// Error: Property 'push' does not exist on type 'readonly ["card", "transfer"]'.
checkout.paymentMethods.push("cash");
