// theme toggle: flips the .dark class and remembers the choice
document.querySelector("[data-theme-toggle]")?.addEventListener("click", () => {
    const dark = document.documentElement.classList.toggle("dark");
    try { localStorage.setItem("theme", dark ? "dark" : "light"); } catch { }
});
