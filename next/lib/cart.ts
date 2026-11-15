import 'server-only';

const apiBase = process.env.BOOKSTORE_API ?? 'http://localhost:5080';

type Cart = { id: string; items: { isbn: string; quantity: number }[] };

async function send<T>(method: string, path: string, body?: unknown): Promise<T> {
  const response = await fetch(`${apiBase}${path}`, {
    method,
    headers: body === undefined ? undefined : { 'Content-Type': 'application/json' },
    body: body === undefined ? undefined : JSON.stringify(body),
  });
  if (!response.ok) {
    throw new Error(`Bookstore.Api answered ${response.status} for ${method} ${path}`);
  }
  return (await response.json()) as T;
}

// The same Bookstore.Api cart endpoints the SPA uses, called from the server
export async function addToServerCart(cartId: string | undefined, isbn: string, quantity: number): Promise<Cart> {
  let cart = cartId ? await fetch(`${apiBase}/api/carts/${cartId}`).then((r) => (r.ok ? (r.json() as Promise<Cart>) : null)) : null;
  cart ??= await send<Cart>('POST', '/api/carts');
  const current = cart.items.find((line) => line.isbn === isbn)?.quantity ?? 0;
  return send<Cart>('PUT', `/api/carts/${cart.id}/items/${isbn}`, { quantity: current + quantity });
}
