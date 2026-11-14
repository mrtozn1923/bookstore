const form = document.querySelector('#checkout');
const summary = document.querySelector('#summary');

// what went wrong and how to fix it, in the words of the label
const rules = {
  email: (v) => (!v ? 'Enter your email address' : /^\S+@\S+\.\S+$/.test(v) ? '' : 'Email address must look like name@example.com'),
  fullName: (v) => (v ? '' : 'Enter your full name'),
  address: (v) => (v ? '' : 'Enter your street address'),
  city: (v) => (v ? '' : 'Enter your city'),
  postcode: (v) => (!v ? 'Enter your postcode' : /^\d{5}$/.test(v) ? '' : 'Postcode must be 5 digits, like 34710'),
};

function setError(input, text) {
  const id = `${input.id}-error`;
  let error = document.getElementById(id);
  if (!text) {
    error?.remove();
    input.removeAttribute('aria-invalid');
    input.setAttribute('aria-describedby', (input.getAttribute('aria-describedby') ?? '').replace(id, '').trim());
    return;
  }
  if (!error) {
    error = document.createElement('p');
    error.id = id;
    error.className = 'error';
    input.before(error);
    input.setAttribute('aria-describedby', `${input.getAttribute('aria-describedby') ?? ''} ${id}`.trim());
  }
  error.textContent = text;
  input.setAttribute('aria-invalid', 'true');
}

// late: check a field when the user leaves it, and only if something was typed
form.addEventListener('focusout', (event) => {
  const input = event.target;
  if (rules[input.name] && input.value) setError(input, rules[input.name](input.value.trim()));
});

// early: once a field shows an error, clear it as soon as the value is fixed
form.addEventListener('input', (event) => {
  const input = event.target;
  if (input.getAttribute('aria-invalid') === 'true') setError(input, rules[input.name](input.value.trim()));
});

document.querySelector('#saveDetails').addEventListener('change', (event) => {
  document.querySelector('#passwordField').hidden = !event.target.checked;
});

form.addEventListener('submit', (event) => {
  event.preventDefault();
  const errors = [];
  for (const [name, rule] of Object.entries(rules)) {
    const input = form.elements[name];
    const text = rule(input.value.trim());
    setError(input, text);
    if (text) errors.push([input.id, text]);
  }
  summary.querySelector('ul').replaceChildren(...errors.map(([id, text]) => {
    const li = document.createElement('li');
    li.innerHTML = `<a href="#${id}"></a>`;
    li.firstChild.textContent = text;
    return li;
  }));
  summary.hidden = errors.length === 0;
  document.title = `${errors.length ? 'Error: ' : ''}Checkout - Bookstore`;
  if (errors.length) {
    summary.focus();
    return;
  }
  document.querySelector('#status').textContent = 'Details saved. Next: payment.';
});
