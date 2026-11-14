const button = document.querySelector("button");

button?.addEventListener("click", (event) => {
  console.log(event.clientX, event.clientY); // event: PointerEvent
});

document.addEventListener("keydown", (event) => {
  if (event.key === "Escape") {
    console.log("Cart closed"); // event: KeyboardEvent
  }
});
