const cartBox = document.querySelector('#cart');
const results = document.querySelector('#results');
const toast = document.querySelector('#toast');
const money = (n) => `₺${n.toFixed(2)}`;
let items = [];
let delivery = 'standard';

function render() {
  cartBox.removeAttribute('aria-busy');
  if (items.length === 0) {
    cartBox.innerHTML = `
      <div class="state">
        <h2>Your cart is empty</h2>
        <p>Books you add stay here until you check out, even if you close the page.</p>
        <div class="actions">
          <a class="btn" href="/web/05-spacing/after.html">Browse the classics</a>
          <a class="btn btn-secondary" href="#q">Search for a book</a>
        </div>
      </div>`;
    return;
  }
  const total = items.reduce((sum, item) => sum + item.price * item.quantity, 0);
  cartBox.innerHTML = `
    ${items.map((item) => `
      <div class="item" data-id="${item.id}">
        <div><div>${item.title}</div><div class="author">${item.author}</div></div>
        <div class="stepper">
          <button class="btn btn-secondary" data-step="-1" aria-label="Decrease quantity of ${item.title}" ${item.quantity === 1 ? 'aria-disabled="true"' : ''}>−</button>
          <output aria-live="polite">${item.quantity}</output>
          <button class="btn btn-secondary" data-step="1" aria-label="Increase quantity of ${item.title}">+</button>
          <button class="btn btn-link remove">Remove</button>
        </div>
        <div class="line-price">${money(item.price * item.quantity)}</div>
        ${item.error ? `<p class="item-error">${item.error}</p>` : ''}
      </div>`).join('')}
    <fieldset>
      <legend>Delivery</legend>
      <label class="option"><input type="radio" name="delivery" value="standard" ${delivery === 'standard' ? 'checked' : ''}> Standard, free, 2–3 working days</label>
      <label class="option"><input type="radio" name="delivery" value="express" ${delivery === 'express' ? 'checked' : ''}> Express, ₺49.90, next working day</label>
    </fieldset>
    <div class="summary">
      <p class="total">Total: ${money(total)}</p>
      <a class="btn checkout" href="/web/06-forms/after.html">Checkout</a>
    </div>`;
}

function renderError() {
  cartBox.removeAttribute('aria-busy');
  cartBox.innerHTML = `
    <div class="state state-error" role="alert">
      <h2>We couldn't load your cart</h2>
      <p>Your books are saved. This is usually a short outage on our side; try again in a moment.</p>
      <div class="actions">
        <button class="btn retry">Try again</button>
        <a class="btn btn-secondary" href="/web/05-spacing/after.html">Keep browsing</a>
      </div>
    </div>`;
}

async function load() {
  try {
    items = await api.getCart();
    render();
  } catch {
    renderError();
  }
}

// optimistic: change the screen now, roll back if the server says no
async function changeQuantity(id, step) {
  const item = items.find((i) => i.id === id);
  const previous = item.quantity;
  if (previous + step < 1) return;
  item.quantity = previous + step;
  item.error = '';
  render();
  try {
    await api.setQuantity(id, item.quantity);
  } catch {
    item.quantity = previous;
    item.error = `Only ${previous} in stock right now. We kept your quantity at ${previous}.`;
    render();
  }
}

// undo instead of "Are you sure?"
let toastTimer;
async function removeItem(id) {
  const index = items.findIndex((i) => i.id === id);
  const [removed] = items.splice(index, 1);
  render();
  showToast(`${removed.title} removed from your cart.`, async () => {
    items.splice(index, 0, removed);
    render();
    await api.restoreItem(removed, index);
  });
  await api.removeItem(id);
}

function showToast(message, onUndo) {
  toast.innerHTML = `<span>${message}</span><button class="btn btn-link">Undo</button>`;
  toast.querySelector('button').addEventListener('click', () => { hideToast(); onUndo(); });
  restartTimer();
}
function hideToast() { toast.innerHTML = ''; }
function restartTimer() { clearTimeout(toastTimer); toastTimer = setTimeout(hideToast, 8000); }
// the toast stays while the pointer or keyboard focus is on it
toast.addEventListener('mouseenter', () => clearTimeout(toastTimer));
toast.addEventListener('focusin', () => clearTimeout(toastTimer));
toast.addEventListener('mouseleave', restartTimer);
toast.addEventListener('focusout', restartTimer);

cartBox.addEventListener('click', (event) => {
  const button = event.target.closest('button');
  if (!button) return;
  if (button.classList.contains('retry')) {
    cartBox.setAttribute('aria-busy', 'true');
    load();
    return;
  }
  if (button.getAttribute('aria-disabled') === 'true') return;
  const id = button.closest('.item')?.dataset.id;
  if (button.dataset.step) changeQuantity(id, Number(button.dataset.step));
  if (button.classList.contains('remove')) removeItem(id);
});
cartBox.addEventListener('change', (event) => {
  if (event.target.name === 'delivery') delivery = event.target.value;
});

// "no results" is a dead end only if we let it be one
const distance = (a, b) => {
  const d = Array.from({ length: a.length + 1 }, (_, i) => [i, ...Array(b.length).fill(0)]);
  for (let j = 1; j <= b.length; j++) d[0][j] = j;
  for (let i = 1; i <= a.length; i++)
    for (let j = 1; j <= b.length; j++)
      d[i][j] = Math.min(d[i - 1][j] + 1, d[i][j - 1] + 1, d[i - 1][j - 1] + (a[i - 1] === b[j - 1] ? 0 : 1));
  return d[a.length][b.length];
};
document.querySelector('#search').addEventListener('submit', async (event) => {
  event.preventDefault();
  const query = event.target.q.value.trim();
  results.innerHTML = '<p aria-busy="true">Searching…</p>';
  const books = await api.search(query);
  if (books.length) {
    results.innerHTML = `<p>${books.length} books: ${books.map((b) => b.title).join(', ')}</p>`;
    return;
  }
  const close = api.catalog.find((b) => b.title.toLowerCase().split(' ').some((w) => distance(w, query.toLowerCase()) <= 2));
  results.innerHTML = `
    <div class="state">
      <h2>No books match "${query}"</h2>
      <p>Check the spelling or search by author.</p>
      <div class="actions">
        ${close ? `<button class="btn suggestion" data-q="${close.title}">Search for "${close.title}"</button>` : ''}
        <a class="btn btn-secondary" href="/web/05-spacing/after.html">Browse the classics</a>
      </div>
    </div>`;
});
results.addEventListener('click', (event) => {
  const button = event.target.closest('.suggestion');
  if (!button) return;
  const form = document.querySelector('#search');
  form.q.value = button.dataset.q;
  form.requestSubmit();
});
load();
