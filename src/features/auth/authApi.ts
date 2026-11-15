import { queryOptions } from '@tanstack/react-query';
import { ApiError, apiFetch } from '@/shared/api';

export type User = { name: string; email: string };

// Who is signed in is known only by the BFF: the session cookie is HttpOnly, JavaScript cannot read it
export const userQuery = queryOptions({
  queryKey: ['user'],
  queryFn: async ({ signal }) => {
    try {
      return await apiFetch<User>('/bff/user', { signal });
    } catch (error) {
      if (error instanceof ApiError && error.status === 401) {
        return null;
      }
      throw error;
    }
  },
  // Changes only on sign-in (a full page load) and sign-out (we update the cache ourselves)
  staleTime: Infinity,
});

export const authApi = {
  signOut: () => apiFetch<void>('/bff/logout', { method: 'POST' }),
};
