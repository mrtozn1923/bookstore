import { createContext, redirect, type MiddlewareFunction } from 'react-router';
import { queryClient } from '@/shared/queryClient';
import { userQuery, type User } from './authApi';

// Typed slot that loaders and actions below this middleware can read
export const userContext = createContext<User>();

// Runs before the route's loader/action on every navigation to it: the counterpart of a CanActivate guard.
// It only decides what to show; the BFF still refuses /api/orders without a session.
export const requireUser: MiddlewareFunction = async ({ request, context }) => {
  const user = await queryClient.ensureQueryData(userQuery);
  if (user === null) {
    const { pathname } = new URL(request.url);
    throw redirect(`/sign-in?redirectTo=${encodeURIComponent(pathname)}`);
  }
  context.set(userContext, user);
};
