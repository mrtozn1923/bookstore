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
      name: 'bookstore.client', // localStorage key
      version: 1,
      // Persist data only; the functions are recreated by the store
      partialize: (state) => ({ cartId: state.cartId, recentlyViewed: state.recentlyViewed }),
    },
  ),
);
