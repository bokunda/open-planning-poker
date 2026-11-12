import { expect, test } from '@playwright/test';
import { expectNoA11yViolations, openApp } from './helpers';

// Runs on desktop and on a phone viewport.
test('the landing page loads, registers the user and is accessible', async ({ page }) => {
  await openApp(page);
  await expect(page.getByRole('button', { name: 'Create game' }).first()).toBeVisible();
  await expectNoA11yViolations(page);
});
