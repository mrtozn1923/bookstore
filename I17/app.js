// I17: page script of index.html (app.js is a module, so open the page through a static server, e.g. `python -m http.server --directory I17 8080`)
// Examples 4, 5, 6, 10, 13 and 14 from the post; the scroll-margin rule (example 11) is in styles.css.

// --- example 4 ---
const reducedMotion = matchMedia("(prefers-reduced-motion: reduce)");
const motionOk = () => !reducedMotion.matches;

console.log(reducedMotion.matches); // false
reducedMotion.addEventListener("change", () => {
  console.log("reduce motion:", reducedMotion.matches);
});

// --- example 5 ---
async function flyToCart(button) {
  if (!motionOk()) return;
  const cover = button.closest(".book").querySelector(".cover");
  const from = cover.getBoundingClientRect();
  const to = document.querySelector("#cart-icon").getBoundingClientRect();

  const ghost = cover.cloneNode(true);
  Object.assign(ghost.style, {
    position: "fixed", left: `${from.left}px`, top: `${from.top}px`,
    width: `${from.width}px`, height: `${from.height}px`, margin: "0", pointerEvents: "none"
  });
  document.body.append(ghost);

  const dx = to.left + to.width / 2 - (from.left + from.width / 2);
  const dy = to.top + to.height / 2 - (from.top + from.height / 2);
  const flight = ghost.animate(
    [
      { transform: "translate(0, 0) scale(1)", opacity: 1 },
      { transform: `translate(${dx}px, ${dy}px) scale(0.1)`, opacity: 0.5 }
    ],
    { duration: 600, easing: "ease-in" }
  );

  try {
    await flight.finished;
  } catch (err) {
    if (err.name !== "AbortError") throw err; // a cancel()ed animation rejects with AbortError
  } finally {
    ghost.remove();
  }
}

// --- example 6 ---
const cart = [];
const cartCount = document.querySelector("#cart-count");

document.querySelector(".shelf").addEventListener("click", async (event) => {
  const button = event.target.closest(".add-to-cart");
  if (!button) return;

  cart.push(button.closest(".book").querySelector(".cover").textContent); // state first, independent of the animation
  await flyToCart(button);
  cartCount.textContent = cart.length;
  if (motionOk()) cartCount.animate([{ transform: "scale(1.5)" }, { transform: "scale(1)" }], 200);
  console.log("in cart:", cart.length, "ghosts left:", document.querySelectorAll("body > .cover").length);
});

// --- example 10 ---
const checkoutForm = document.querySelector("#checkout");

function focusFirstError(form) {
  const field = form.querySelector(":invalid");
  if (!field) return null;
  field.scrollIntoView({ block: "center", behavior: motionOk() ? "smooth" : "instant" });
  field.focus({ preventScroll: true });
  return field;
}

checkoutForm.addEventListener("submit", (event) => {
  if (checkoutForm.checkValidity()) return;
  event.preventDefault();
  const field = focusFirstError(checkoutForm);
  console.log("first error:", field.id, "focused:", document.activeElement === field);
});

// --- example 13 ---
const shelf = document.querySelector(".shelf");
const prevButton = document.querySelector("#shelf-prev");
const nextButton = document.querySelector("#shelf-next");

function updateArrows() {
  const maxScroll = shelf.scrollWidth - shelf.clientWidth;
  prevButton.disabled = shelf.scrollLeft <= 0;
  nextButton.disabled = shelf.scrollLeft >= maxScroll - 1;
  console.log("scrollLeft:", shelf.scrollLeft, "prev:", !prevButton.disabled, "next:", !nextButton.disabled);
}

function scrollShelf(direction) {
  shelf.scrollBy({ left: direction * shelf.clientWidth, behavior: motionOk() ? "smooth" : "instant" });
}

prevButton.addEventListener("click", () => scrollShelf(-1));
nextButton.addEventListener("click", () => scrollShelf(1));
shelf.addEventListener("scrollend", updateArrows);
updateArrows(); // scrollLeft: 0 prev: false next: true

// --- example 14 ---
if (!("onscrollend" in window)) {
  let scrollTimer;
  shelf.addEventListener("scroll", () => {
    clearTimeout(scrollTimer);
    scrollTimer = setTimeout(updateArrows, 150);
  }, { passive: true });
}
