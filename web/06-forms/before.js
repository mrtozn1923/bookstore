const form = document.querySelector('#checkout');
const rules = {
  email: (v) => /^\S+@\S+\.\S+$/.test(v),
  postcode: (v) => /^\d{5}$/.test(v),
};

// validates on every keystroke
form.addEventListener('input', (event) => {
  const input = event.target;
  const rule = rules[input.name];
  if (!rule) return;
  showError(input, rule(input.value) ? '' : 'Invalid');
});

form.addEventListener('submit', (event) => {
  event.preventDefault();
  for (const input of form.querySelectorAll('input')) {
    const required = input.labels[0].textContent.endsWith('*');
    if (required && !input.value) showError(input, 'Invalid');
  }
});

function showError(input, text) {
  let error = input.parentElement.querySelector('.error');
  if (!error) {
    error = document.createElement('span');
    error.className = 'error';
    input.parentElement.append(error);
  }
  error.textContent = text;
}
