import { create } from 'zustand';

export type User = { name: string; email: string };

type AuthState = {
  user: User | null;
  signIn: (user: User) => void;
  signOut: () => void;
};

// Demo sign-in, in memory only. Components select from it; router middleware reads useAuthStore.getState()
export const useAuthStore = create<AuthState>()((set) => ({
  user: null,
  signIn: (user) => set({ user }),
  signOut: () => set({ user: null }),
}));
