// Demo sign-in kept outside React: components read it with useSyncExternalStore, router middleware with get()
export type User = { name: string; email: string };

let user: User | null = null;
const listeners = new Set<() => void>();

export const authStore = {
  get: () => user,
  subscribe(listener: () => void) {
    listeners.add(listener);
    return () => listeners.delete(listener);
  },
  signIn(next: User) {
    user = next;
    listeners.forEach((l) => l());
  },
  signOut() {
    user = null;
    listeners.forEach((l) => l());
  },
};
