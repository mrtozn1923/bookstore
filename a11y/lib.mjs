// Shared helpers for the lab scripts: base URL and Bookstore's pages, ready to test.
export const base = process.env.BOOKSTORE_URL ?? 'http://127.0.0.1:52411';

export const pages = {
  catalog: { path: '/', ready: '.book-card' },
  book: { path: '/books/9780441172719', ready: 'h1' },
  cart: { path: '/cart', ready: 'table', async setup(page) {
    // The cart must not be empty: add the first book of the catalog (1984)
    await page.goto(base + '/');
    await page.locator('.book-card').first().getByRole('button', { name: /Add to cart/ }).click();
    await page.getByTestId('cart-link').filter({ hasText: '(1)' }).waitFor();
  } },
  statement: { path: '/accessibility', ready: 'h1' },
};

export async function open(page, name) {
  const p = pages[name];
  await p.setup?.(page);
  await page.goto(base + p.path);
  await page.locator(p.ready).first().waitFor();
  return p;
}

// Role, name, description and invalid state of the focused element, from Chromium's accessibility tree
export async function focusedAx(page) {
  const cdp = await page.context().newCDPSession(page);
  const { nodes } = await cdp.send('Accessibility.getFullAXTree');
  await cdp.detach();
  const node = nodes.findLast(n => n.role?.value !== 'RootWebArea' && n.properties?.some(p => p.name === 'focused' && p.value.value));
  if (!node) return '(nothing focused)';
  const prop = name => node.properties.find(p => p.name === name)?.value.value;
  return `${node.role.value} "${node.name?.value ?? ''}"` +
    (node.description?.value ? ` description="${node.description.value}"` : '') +
    (prop('invalid') && prop('invalid') !== 'false' ? ` invalid=${prop('invalid')}` : '');
}
