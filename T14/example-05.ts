const email = document.querySelector("#email");

if (email instanceof HTMLInputElement) {
  console.log(email.value.trim());
} else {
  console.error("Missing #email input");
}
