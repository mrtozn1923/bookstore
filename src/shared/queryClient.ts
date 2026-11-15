import { QueryClient } from '@tanstack/react-query';

// One cache for the whole app; data counts as fresh for 30 seconds.
// A module, not a component: route loaders and actions run outside React and need it too.
export const queryClient = new QueryClient({
  defaultOptions: { queries: { staleTime: 30_000 } },
});
