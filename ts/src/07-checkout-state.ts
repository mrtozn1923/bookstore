import { computed, signal } from "@angular/core";
import { banner, toCheckoutState, type CheckoutState } from "./checkout-state.ts";

// Start the API first: dotnet run --project src/Bookstore.Fp.Api --urls http://127.0.0.1:52107
const apiUrl = process.env.BOOKSTORE_API ?? "http://127.0.0.1:52107";

const state = signal<CheckoutState>({ kind: "idle" });
const message = computed(() => banner(state()));

const requests = [
  { email: "alan@example.com", isbn: "9780141439587", quantity: 1 },
  { email: "alan.example.com", isbn: "9780141439580", quantity: 1 },
  { email: "alan@example.com", isbn: "9780261103344", quantity: 1 },
];

for (const request of requests) {
  const response = await fetch(`${apiUrl}/orders`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(request),
  });
  state.set(toCheckoutState(response.status, await response.json()));
  console.log(`${response.status} ${state().kind.padEnd(8)} ${message()}`);
}
