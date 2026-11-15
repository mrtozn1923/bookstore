import type { APIRequestContext, PlaywrightWorkerArgs } from '@playwright/test';
import { apiURL } from './config';

// E2E host'unun token endpoint'inden token alıp onu taşıyan bir API context'i açar
export async function signedInApi(playwright: PlaywrightWorkerArgs['playwright'], body: object): Promise<APIRequestContext> {
  const anonymous = await playwright.request.newContext({ baseURL: apiURL });
  const { accessToken } = await (await anonymous.post('/e2e/token', { data: body })).json();
  await anonymous.dispose();
  return playwright.request.newContext({ baseURL: apiURL, extraHTTPHeaders: { Authorization: `Bearer ${accessToken}` } });
}
