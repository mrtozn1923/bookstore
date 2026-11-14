// Shows "Read more" only when the clamped summary really hides text.
for (const button of document.querySelectorAll('.read-more')) {
    const summary = document.getElementById(button.getAttribute('aria-controls'));
    const update = () => {
        if (summary.classList.contains('is-clamped')) {
            button.hidden = summary.scrollHeight <= summary.clientHeight;
        }
    };
    // runs again after styles, fonts or the viewport width change the layout
    new ResizeObserver(update).observe(summary);
    button.addEventListener('click', () => {
        const expanded = summary.classList.toggle('is-clamped') === false;
        button.setAttribute('aria-expanded', String(expanded));
        button.textContent = expanded ? 'Show less' : 'Read more';
    });
}
