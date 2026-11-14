function handleKey(event: KeyboardEvent) {
  if (event.key === "Escape") {
    console.log("Cart closed");
  }
}

document.addEventListener("keydown", handleKey);

// Error: No overload matches this call.
document.addEventListener("click", handleKey);
