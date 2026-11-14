import { signedInState } from './auth-state';
import { expect, test as setup } from './fixtures';

setup('sign in once', async ({ page, api }) => {
  await page.goto('/sign-in');
  await page.getByLabel('Email').fill('ada@example.com');
  await page.getByLabel('Password').fill('not-checked-by-the-mock');
  await page.getByRole('button', { name: 'Sign in' }).click();
  await expect(page).toHaveURL('/');

  await page.context().storageState({ path: signedInState });
});
