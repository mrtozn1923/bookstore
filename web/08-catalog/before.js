const state = { categories: new Set(), formats: new Set(), sort: 'relevance', q: '' };
let shown = 0;
const grid = document.querySelector('#grid');
const money = (n) => `₺${n.toFixed(2)}`;

document.querySelector('#categories').innerHTML = categories.map((c) => `<label><input type="checkbox" value="${c.slug}" data-facet="categories"> ${c.name}</label>`).join('');
document.querySelector('#formats').innerHTML = Object.entries(formatNames).map(([k, v]) => `<label><input type="checkbox" value="${k}" data-facet="formats"> ${v}</label>`).join('');

function results() {
  const list = books.filter((b) =>
    (!state.categories.size || state.categories.has(b.category)) &&
    (!state.formats.size || state.formats.has(b.format)) &&
    `${b.title} ${b.author}`.toLowerCase().includes(state.q.toLowerCase()));
  const by = { 'price-asc': (a, b) => a.price - b.price, 'price-desc': (a, b) => b.price - a.price, 'year-desc': (a, b) => b.year - a.year };
  return by[state.sort] ? list.sort(by[state.sort]) : list;
}

// filter state lives only in this variable: the URL never changes
function render() {
  shown = 0;
  grid.innerHTML = '';
  more();
}
function more() {
  const list = results();
  if (!list.length) grid.innerHTML = '<p>No results</p>';
  for (const b of list.slice(shown, shown + 12)) {
    grid.insertAdjacentHTML('beforeend', `<div class="card" onclick="location.href='/web/02-book-detail/after.html'">
      <div class="title">${b.title}</div><div class="meta">${b.author}</div><div class="meta">${formatNames[b.format]}</div><div>${money(b.price)}</div></div>`);
  }
  shown += 12;
}

// infinite scroll: 12 more books whenever the bottom of the list comes into view
new IntersectionObserver((entries) => {
  if (entries[0].isIntersecting && shown < results().length) more();
}, { rootMargin: '200px' }).observe(document.querySelector('#sentinel'));

document.querySelector('.filters').addEventListener('change', (event) => {
  const input = event.target;
  if (input.id === 'sort') state.sort = input.value;
  else state[input.dataset.facet][input.checked ? 'add' : 'delete'](input.value);
  render();
});
document.querySelector('#q').addEventListener('keydown', (event) => {
  if (event.key === 'Enter') { state.q = event.target.value; render(); }
});
render();
