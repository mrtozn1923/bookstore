const list = document.querySelector("ul");

list?.addEventListener("click", (event) => {
  // Error: 'event.target' is possibly 'null'.
  // Error: Property 'closest' does not exist on type 'EventTarget'.
  const item = event.target.closest("li");
});
