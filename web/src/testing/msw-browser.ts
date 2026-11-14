import { setupWorker } from 'msw/browser';
import { handlers } from '../mocks/handlers';

export const worker = setupWorker(...handlers);

export function useMockApiInBrowser(): void {
  beforeAll(() => worker.start({ quiet: true, onUnhandledFrame: 'error' }));
  afterEach(() => worker.resetHandlers());
  afterAll(() => worker.stop());
}
