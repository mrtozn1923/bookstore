import '@testing-library/jest-dom/vitest';
import { cleanup } from '@testing-library/react';
import { afterAll, afterEach, beforeAll } from 'vitest';
import { useClientStore } from '@/shared/clientStore';
import { queryClient } from '@/shared/queryClient';
import { carts } from '@/test/handlers';
import { server } from '@/test/server';

// A request without a handler fails the test instead of going to the real network
beforeAll(() => server.listen({ onUnhandledFrame: 'error' }));

afterEach(() => {
  cleanup();
  server.resetHandlers();
  carts.clear();
  // Module-level stores and caches outlive a test: put them back to their initial state
  queryClient.clear();
  useClientStore.setState(useClientStore.getInitialState(), true);
  localStorage.clear();
});

afterAll(() => server.close());
