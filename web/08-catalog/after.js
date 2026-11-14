const PAGE_SIZE = 12;
const money = (n) => `₺${n.toFixed(2)}`;
const $ = (sel) => document.querySelector(sel);

// the URL is the single source of truth for what the list shows
function readState() {
  const p = new URLSearchParams(location.search);
  return {
    category: p.get('category') ?? '',
    formats: p.getAll('format'),
    sort: p.get('sort') ?? 'relevance',
    q: p.get('q') ?? '',
    page: Math.max(1, Number(p.get('page')) || 1),
  };
}
function toUrl(s) {
  const p = new URLSearchParams();
  if (s.category) p.set('category', s.category);
  for (const f of s.formats) p.append('format', f);
  if (s.q) p.set('q', s.q);
  if (s.sort !== 'relevance') p.set('sort', s.sort);
  if (s.page > 1) p.set('page', s.page);
  const query = p.toString();
  return query ? `?${query}` : location.pathname;
}

function filter(s, list = books) {
  const by = { 'price-asc': (a, b) => a.price - b.price, 'price-desc': (a, b) => b.price - a.price, 'year-desc': (a, b) => b.year - a.year };
  const out = list.filter((b) =>
    (!s.category || b.category === s.category) &&
    (!s.formats.length || s.formats.includes(b.format)) &&
    `${b.title} ${b.author}`.toLowerCase().includes(s.q.toLowerCase()));
  return by[s.sort] ? out.sort(by[s.sort]) : out;
}

function render() {
  const s = readState();
  const list = filter(s);
  const pages = Math.max(1, Math.ceil(list.length / PAGE_SIZE));
  const category = categories.find((c) => c.slug === s.category);
  const title = s.q ? `Results for "${s.q}"` : category?.name ?? 'All books';
  document.title = `${title} - Bookstore`;
  $('#title').textContent = title;
  $('#q').value = s.q;
  $('#sort').value = s.sort;

  $('#cats').innerHTML = categories.map((c) =>
    `<a href="${toUrl({ ...s, category: c.slug, formats: [], q: '', page: 1 })}" ${c.slug === s.category ? 'aria-current="page"' : ''}>${c.name}</a>`).join('');

  // breadcrumb: where this list sits in the hierarchy, not the path the user took
  const crumbs = [`<li><a href="after.html">Home</a></li>`];
  if (category && s.q) crumbs.push(`<li><a href="${toUrl({ ...s, q: '', formats: [], page: 1 })}">${category.name}</a></li>`);
  crumbs.push(`<li aria-current="page">${title}</li>`);
  $('#crumbs').innerHTML = crumbs.join('');

  // facet counts: how many books each option would show with the other filters kept
  $('#formats').innerHTML = Object.entries(formatNames).map(([key, name]) => {
    const n = filter({ ...s, formats: [key] }).length;
    return `<label class="option"><input type="checkbox" name="format" value="${key}" ${s.formats.includes(key) ? 'checked' : ''}> ${name} <span class="n">(${n})</span></label>`;
  }).join('');

  const from = (s.page - 1) * PAGE_SIZE;
  $('#count').textContent = list.length ? `${list.length} books, showing ${from + 1}–${Math.min(from + PAGE_SIZE, list.length)}` : '';
  $('#chips').innerHTML = [
    ...(category ? [[`Category: ${category.name}`, { ...s, category: '', page: 1 }]] : []),
    ...s.formats.map((f) => [`Format: ${formatNames[f]}`, { ...s, formats: s.formats.filter((x) => x !== f), page: 1 }]),
    ...(s.q ? [[`Search: ${s.q}`, { ...s, q: '', page: 1 }]] : []),
  ].map(([label, next]) => `<li><a class="chip" href="${toUrl(next)}" aria-label="Remove filter ${label}">${label} ×</a></li>`)
    .concat(category || s.formats.length || s.q ? [`<li><a class="chip" href="after.html">Clear all</a></li>`] : []).join('');

  $('#grid').innerHTML = list.length ? list.slice(from, from + PAGE_SIZE).map((b) => `
    <a class="card" href="/web/02-book-detail/after.html">
      <div class="title">${b.title}</div><div class="meta">${b.author}</div><div class="meta">${formatNames[b.format]}</div><div class="price">${money(b.price)}</div>
    </a>`).join('') : noResults(s);

  $('#pages').innerHTML = pages > 1 ? Array.from({ length: pages }, (_, i) => i + 1).map((n) => n === s.page
    ? `<span aria-current="page">${n}</span>`
    : `<a href="${toUrl({ ...s, page: n })}">${n}</a>`).join('') : '';
}

// "no results" with filters: show what each filter costs
function noResults(s) {
  const options = [
    ...(s.category ? [[`Search all categories`, { ...s, category: '' }]] : []),
    ...s.formats.map((f) => [`Remove "${formatNames[f]}"`, { ...s, formats: s.formats.filter((x) => x !== f) }]),
  ].map(([label, next]) => [label, next, filter(next).length]).filter(([, , n]) => n > 0);
  const plural = (n) => `${n} book${n === 1 ? '' : 's'}`;
  return `<div class="empty"><h2>No books match these filters</h2>
    <ul>${options.map(([label, next, n]) => `<li><a href="${toUrl({ ...next, page: 1 })}">${label}</a> (${plural(n)})</li>`).join('')}
      <li><a href="after.html">Clear all filters</a> (${plural(books.length)})</li></ul></div>`;
}

// a filter change is a step the user may want to undo with Back: pushState
$('#filters').addEventListener('change', () => {
  const s = readState();
  const formats = [...document.querySelectorAll('[name=format]:checked')].map((i) => i.value);
  history.pushState(null, '', toUrl({ ...s, formats, page: 1 }));
  render();
});
// a different sort order of the same list is not: replaceState
$('#sort').addEventListener('change', (event) => {
  history.replaceState(null, '', toUrl({ ...readState(), sort: event.target.value, page: 1 }));
  render();
});
window.addEventListener('popstate', render);

$('#suggestions').innerHTML = [...new Set(books.flatMap((b) => [b.title, b.author]))].map((v) => `<option value="${v}">`).join('');
render();
