const list = document.querySelector("ul");

list?.addEventListener("click", (event) => {
  if (event.target instanceof Element) {
    const item = event.target.closest("li");
    console.log(item?.textContent);
  }
});
