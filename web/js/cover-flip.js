// Turns the cover over; aria-pressed tells screen readers which side is up.
const flip = document.querySelector('.cover-flip');
const flipButton = document.querySelector('.flip-button');
flipButton?.addEventListener('click', () => {
    const flipped = flip.classList.toggle('is-flipped');
    flipButton.setAttribute('aria-pressed', String(flipped));
});
