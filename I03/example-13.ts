interface CheckoutSettings {
  readonly shipping: { method: string };
}

const checkout: Readonly<CheckoutSettings> = { shipping: { method: "standard" } };
checkout.shipping.method = "express"; // compiles: the inner object isn't readonly
