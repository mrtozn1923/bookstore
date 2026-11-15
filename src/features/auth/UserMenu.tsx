import { Link } from 'react-router';
import { useAuthStore } from '@/features/auth/authStore';

export function UserMenu() {
  const user = useAuthStore((state) => state.user);
  const signOut = useAuthStore((state) => state.signOut);

  if (user === null) {
    return <Link to="/sign-in">Sign in</Link>;
  }
  return (
    <span>
      {user.name}{' '}
      <button type="button" onClick={signOut}>Sign out</button>
    </span>
  );
}
