import { create } from 'zustand';
import { persist } from 'zustand/middleware';

export type RecentBook = { isbn: string; title: string };

type ClientState = {
  cartId: string | null;
  recentlyViewed: RecentBook[];
  setCartId: (cartId: string | null) => void;
  addRecentlyViewed: (book: RecentBook) => void;
  clearRecentlyViewed: () => void;
};

const storageKey = 'bookstore.client';
const legacyCartIdKey = 'bookstore.cartId';

// One-time migration: up to post 10 the cart id had a key of its own. Without this, users who
// already have a cart would lose it (the server keeps it, but this browser would forget its id).
export function migrateLegacyCartId(storage: Storage) {
  const legacyCartId = storage.getItem(legacyCartIdKey);
  if (legacyCartId === null) {
    return;
  }
  if (storage.getItem(storageKey) === null) {
    // The shape persist writes: { state, version }
    storage.setItem(storageKey, JSON.stringify({ state: { cartId: legacyCartId, recentlyViewed: [] }, version: 1 }));
  }
  storage.removeItem(legacyCartIdKey);
}

// Runs once when the module loads, before the store below reads localStorage
migrateLegacyCartId(localStorage);

// Client state that should survive a reload: which server cart is ours, and the last books opened
export const useClientStore = create<ClientState>()(
  persist(
    (set) => ({
      cartId: null,
      recentlyViewed: [],
      setCartId: (cartId) => set({ cartId }),
      addRecentlyViewed: (book) =>
        set((state) => ({
          recentlyViewed: [book, ...state.recentlyViewed.filter((b) => b.isbn !== book.isbn)].slice(0, 5),
        })),
      clearRecentlyViewed: () => set({ recentlyViewed: [] }),
    }),
    {
      name: storageKey, // localStorage key
      version: 1,
      // Persist data only; the functions are recreated by the store
      partialize: (state) => ({ cartId: state.cartId, recentlyViewed: state.recentlyViewed }),
    },
  ),
);
