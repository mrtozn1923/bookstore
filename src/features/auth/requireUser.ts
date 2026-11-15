import { createContext, redirect, type MiddlewareFunction } from 'react-router';
import { authStore, type User } from '@/features/auth/authStore';

// Typed slot that loaders and actions below this middleware can read
export const userContext = createContext<User>();

// Runs before the route's loader/action on every navigation to it: the counterpart of a CanActivate guard
export const requireUser: MiddlewareFunction = ({ request, context }) => {
  const user = authStore.get();
  if (user === null) {
    const { pathname } = new URL(request.url);
    throw redirect(`/sign-in?redirectTo=${encodeURIComponent(pathname)}`);
  }
  context.set(userContext, user);
};
