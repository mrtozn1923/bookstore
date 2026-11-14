const form = document.querySelector('#checkout');
const message = document.querySelector('#message');

form.addEventListener('submit', async (event) => {
  event.preventDefault();
  const data = Object.fromEntries(new FormData(form));
  if (!/^\d{5}$/.test(data.postcode)) {
    message.textContent = 'Invalid input.';
    message.hidden = false;
    form.reset();
    return;
  }
  const order = await window.placeOrder(data);
  message.textContent = `Purchase complete: ${order.id}`;
  message.hidden = false;
});
