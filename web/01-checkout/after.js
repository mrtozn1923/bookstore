const form = document.querySelector('#checkout');
const pay = document.querySelector('#pay');
const status = document.querySelector('#status');
const postcode = document.querySelector('#postcode');
const postcodeError = document.querySelector('#postcode-error');
const subtotal = 835.0;
const money = (value) => `₺${value.toFixed(2)}`;
let submitting = false;

function updateTotal() {
  const delivery = Number(form.elements.shipping.value);
  document.querySelector('#delivery').textContent = delivery === 0 ? 'Free' : money(delivery);
  document.querySelector('#total').textContent = money(subtotal + delivery);
  pay.textContent = `Pay ${money(subtotal + delivery)}`;
}

form.addEventListener('change', updateTotal);

form.addEventListener('submit', async (event) => {
  event.preventDefault();
  if (submitting) return; // the second click while the request runs does nothing
  if (!/^\d{5}$/.test(postcode.value.trim())) {
    postcodeError.textContent = 'Enter a 5-digit postcode, like 34710';
    postcodeError.hidden = false;
    postcode.setAttribute('aria-invalid', 'true');
    postcode.focus();
    return;
  }
  postcodeError.hidden = true;
  postcode.removeAttribute('aria-invalid');
  submitting = true;
  pay.setAttribute('aria-disabled', 'true');
  pay.textContent = 'Processing payment...';
  const order = await window.placeOrder(Object.fromEntries(new FormData(form)));
  status.textContent = `Order ${order.id} placed. A confirmation email is on its way.`;
  pay.textContent = 'Paid';
});
