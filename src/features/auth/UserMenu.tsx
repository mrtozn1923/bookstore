import { useQuery, useQueryClient } from '@tanstack/react-query';
import { Link, useNavigate } from 'react-router';
import { authApi, userQuery } from './authApi';

export function UserMenu() {
  const { data: user } = useQuery(userQuery);
  const queryClient = useQueryClient();
  const navigate = useNavigate();

  if (user === undefined) {
    return null; // not known yet
  }
  if (user === null) {
    return <Link to="/sign-in">Sign in</Link>;
  }

  async function handleSignOut() {
    await authApi.signOut();
    queryClient.setQueryData(userQuery.queryKey, null);
    navigate('/');
  }

  return (
    <span>
      {user.name}{' '}
      <button type="button" onClick={handleSignOut}>Sign out</button>
    </span>
  );
}
