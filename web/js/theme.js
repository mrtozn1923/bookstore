// The user's choice wins over the system preference; without a saved choice the page follows the system.
const root = document.documentElement;
const button = document.querySelector('.theme-toggle');

const isDark = () => root.dataset.theme
    ? root.dataset.theme === 'dark'
    : matchMedia('(prefers-color-scheme: dark)').matches;

const render = () => button.setAttribute('aria-pressed', String(isDark()));

button.addEventListener('click', () => {
    root.dataset.theme = isDark() ? 'light' : 'dark';
    try {
        localStorage.setItem('theme', root.dataset.theme);
    } catch {
        // storage can be blocked (private mode, policies); the choice then lasts until reload
    }
    render();
});

// the system preference can change while the page is open (e.g. automatic dark mode at sunset)
matchMedia('(prefers-color-scheme: dark)').addEventListener('change', render);
render();
