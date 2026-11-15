import { createContext, use } from 'react';

export type User = { name: string; email: string };

export type AuthContextValue = {
  user: User | null;
  signIn: (user: User) => void;
  signOut: () => void;
};

export const AuthContext = createContext<AuthContextValue | null>(null);

export function useAuth(): AuthContextValue {
  const auth = use(AuthContext);
  if (auth === null) {
    throw new Error('useAuth must be used inside <AuthProvider>');
  }
  return auth;
}
