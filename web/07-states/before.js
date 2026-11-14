const overlay = document.querySelector('#overlay');
const cartBox = document.querySelector('#cart');
const money = (n) => `₺${n.toFixed(2)}`;

function render(items) {
  const total = items.reduce((sum, item) => sum + item.price * item.quantity, 0);
  cartBox.innerHTML = `
    <table>
      <tr><th>Book</th><th>Quantity</th><th>Price</th><th></th></tr>
      ${items.map((item) => `
        <tr data-id="${item.id}">
          <td>${item.title}</td>
          <td class="qty"><button data-step="-1">-</button> <span class="count">${item.quantity}</span> <button data-step="1">+</button></td>
          <td>${money(item.price * item.quantity)}</td>
          <td><button class="remove">Remove</button></td>
        </tr>`).join('')}
    </table>
    <p class="total">Total: ${money(total)}</p>
    <select id="delivery">
      <option value="">Choose...</option>
      <option>Standard</option><option>Express</option><option>Same day</option>
      <option>Pick-up point</option><option>Store pick-up</option>
    </select>
    <button class="checkout" disabled>Checkout</button>`;
}

async function load() {
  overlay.hidden = false;
  try {
    render(await api.getCart());
  } catch (error) {
    alert(`Error: ${error.message}`);
  }
  overlay.hidden = true;
}

cartBox.addEventListener('click', async (event) => {
  const row = event.target.closest('tr');
  if (event.target.dataset.step) {
    const quantity = Number(row.querySelector('.count').textContent) + Number(event.target.dataset.step);
    render(await api.setQuantity(row.dataset.id, quantity)); // the screen changes when the server answers
  }
  if (event.target.classList.contains('remove') && confirm('Are you sure?')) {
    render(await api.removeItem(row.dataset.id));
  }
});
cartBox.addEventListener('change', (event) => {
  if (event.target.id === 'delivery') cartBox.querySelector('.checkout').disabled = !event.target.value;
});
document.querySelector('#search').addEventListener('submit', async (event) => {
  event.preventDefault();
  const books = await api.search(event.target.q.value);
  document.querySelector('#results').textContent = `${books.length} results`;
});
load();
