import { Link } from 'react-router';
import { useAuth } from '@/features/auth/useAuth';

export function UserMenu() {
  const { user, signOut } = useAuth();

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
