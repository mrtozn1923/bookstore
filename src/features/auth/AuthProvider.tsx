import { useState, type ReactNode } from 'react';
import { AuthContext, type AuthContextValue, type User } from '@/features/auth/AuthContext';

// Demo only: the user lives in memory. Real sign-in (cookie + BFF) comes later in the series.
export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<User | null>(null);

  const value: AuthContextValue = {
    user,
    signIn: setUser,
    signOut: () => setUser(null),
  };
  return <AuthContext value={value}>{children}</AuthContext>;
}
