// Opens the cart drawer as a modal dialog; Esc and the Close button (method="dialog") close it.
const drawer = document.querySelector('#cart-drawer');
document.querySelector('[aria-controls="cart-drawer"]')?.addEventListener('click', () => drawer.showModal());
