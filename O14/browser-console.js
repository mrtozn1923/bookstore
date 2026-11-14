// O14: examples that need a browser; paste these blocks into the DevTools console one by one
// (if a block uses a function from examples.js, paste that file first)

// --- example 1 ---
const input = document.createElement("input");
input.type = "email";
input.required = true;

input.value = "ada";
console.log(input.validity.valid, input.validity.typeMismatch);

input.setCustomValidity("This address is not accepted");
input.value = "ada@example.com";
console.log(input.validity.valid);
