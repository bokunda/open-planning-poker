import AxeBuilder from '@axe-core/playwright';
import { Browser, expect, Page } from '@playwright/test';

/** Opens the app and waits until the anonymous user is registered (the token is in localStorage). */
export async function openApp(page: Page, path = '/game'): Promise<void> {
  await page.goto(path);
  await expect.poll(() => page.evaluate(() => localStorage.getItem('token')), { message: 'anonymous user registered' }).not.toBeNull();
}

/** Creates a game from the landing page and returns its id (the room URL is /game/:id). */
export async function createGame(page: Page, name = 'Sprint 42 refinement'): Promise<string> {
  await openApp(page);
  await page.getByRole('button', { name: 'Create game' }).first().click();
  const dialog = page.getByRole('dialog');
  await dialog.getByLabel('Game name').fill(name);
  await dialog.getByRole('button', { name: 'Create game' }).click();
  await page.waitForURL(/\/game\/[0-9a-f-]{36}/);
  return page.url().split('/game/')[1].split(/[?#]/)[0];
}

/** A second person in their own browser context (own storage, own anonymous user) who opens the game link. */
export async function joinAsSecondPlayer(browser: Browser, gameId: string): Promise<Page> {
  const context = await browser.newContext();
  const page = await context.newPage();
  await openApp(page, `/game/${gameId}`);
  await expect(page.getByRole('region', { name: 'Voting area' })).toBeVisible();
  return page;
}

export async function addTicket(page: Page, name: string): Promise<void> {
  await page.getByRole('button', { name: 'New ticket' }).first().click();
  const dialog = page.getByRole('dialog');
  await dialog.getByLabel('Ticket').fill(name);
  await dialog.getByRole('button', { name: 'Add ticket' }).click();
  await expect(dialog).toBeHidden();
}

export async function vote(page: Page, value: string): Promise<void> {
  await page.getByRole('button', { name: `Vote ${value}`, exact: true }).click();
}

/** Fails on any WCAG 2 A/AA violation on the current page. */
export async function expectNoA11yViolations(page: Page): Promise<void> {
  const results = await new AxeBuilder({ page }).withTags(['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa']).analyze();
  const summary = results.violations.map((v) => `${v.id} (${v.impact}): ${v.help} — ${v.nodes.map((n) => n.target.join(' ')).slice(0, 3).join(', ')}`);
  expect(summary, 'axe WCAG 2 A/AA violations').toEqual([]);
}
