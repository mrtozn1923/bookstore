import { useAuth } from '@/features/auth/AuthContext';

export function UserMenu() {
  const { user, signIn, signOut } = useAuth();

  if (user === null) {
    return (
      <button type="button" onClick={() => signIn({ name: 'Ada Lovelace', email: 'ada@example.com' })}>
        Sign in as Ada
      </button>
    );
  }
  return (
    <span>
      {user.name}{' '}
      <button type="button" onClick={signOut}>Sign out</button>
    </span>
  );
}
