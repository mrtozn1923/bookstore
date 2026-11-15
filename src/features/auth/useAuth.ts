import { useSyncExternalStore } from 'react';
import { authStore } from '@/features/auth/authStore';

export function useAuth() {
  const user = useSyncExternalStore(authStore.subscribe, authStore.get);
  return { user, signIn: authStore.signIn, signOut: authStore.signOut };
}
