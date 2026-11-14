// Toggles the heart; the filled look comes from CSS on aria-pressed="true".
for (const button of document.querySelectorAll('.fav')) {
    button.addEventListener('click', () => {
        button.setAttribute('aria-pressed', String(button.getAttribute('aria-pressed') !== 'true'));
    });
}
