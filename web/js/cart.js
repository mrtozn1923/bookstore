// Adds the book to the cart and gives the cart icon a short swing.
const icon = document.querySelector('.cart-icon');
const count = document.querySelector('.cart-count');
document.querySelector('.add-to-cart')?.addEventListener('click', () => {
    count.textContent = String(Number(count.textContent) + 1);
    icon.classList.remove('bump');
    void icon.getBoundingClientRect(); // restart the animation on every click
    icon.classList.add('bump');
});
icon?.addEventListener('animationend', () => icon.classList.remove('bump'));
