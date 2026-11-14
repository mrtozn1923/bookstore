// Sorts the book list in place; where view transitions exist, every card moves from its old place to its new one.
const list = document.querySelector('.book-list');
const select = document.querySelector('.catalog-toolbar select');

const title = card => card.querySelector('.book-title').textContent;
const price = card => parseFloat(card.querySelector('.price').textContent);

function sortBooks() {
    const cards = [...list.children].sort((a, b) =>
        select.value === 'Price' ? price(a) - price(b) : title(a).localeCompare(title(b), 'en'));
    list.append(...cards);
}

select.addEventListener('change', () => {
    // older browsers: sort without an animation
    if (!document.startViewTransition) {
        sortBooks();
        return;
    }
    document.startViewTransition(sortBooks);
});
