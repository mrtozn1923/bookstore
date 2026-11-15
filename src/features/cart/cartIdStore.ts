// Which server-side cart belongs to this browser; outside React so that route actions can read it
const storageKey = 'bookstore.cartId';
const listeners = new Set<() => void>();

export const cartIdStore = {
  get: () => localStorage.getItem(storageKey),
  subscribe(listener: () => void) {
    listeners.add(listener);
    return () => listeners.delete(listener);
  },
  set(cartId: string | null) {
    if (cartId === null) {
      localStorage.removeItem(storageKey);
    } else {
      localStorage.setItem(storageKey, cartId);
    }
    listeners.forEach((l) => l());
  },
};
