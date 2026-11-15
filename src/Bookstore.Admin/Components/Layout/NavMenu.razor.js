// Closes the mobile navigation menu after a click, as the .NET 11 template does (no inline onclick handler).
const navScrollable = document.getElementById("nav-scrollable");
const navToggler = document.querySelector(".navbar-toggler");
if (navScrollable && navToggler) {
    navScrollable.addEventListener("click", () => navToggler.click());
}
