import { setupServer } from 'msw/node';
import { handlers } from '@/test/handlers';

// MSW in Node: intercepts fetch at the network level, the app's apiFetch runs unchanged
export const server = setupServer(...handlers);
