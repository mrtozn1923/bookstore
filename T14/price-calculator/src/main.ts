// src/main.ts
import { formatPrice } from "./format.ts";

const unitPrice = 450;
const quantity = document.querySelector("#quantity");
const total = document.querySelector("#total");

if (quantity instanceof HTMLInputElement && total instanceof HTMLOutputElement) {
  quantity.addEventListener("input", () => {
    total.value = formatPrice(quantity.valueAsNumber * unitPrice);
  });
}
