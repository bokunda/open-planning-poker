import { expect, test } from '@playwright/test';
import { openApp } from './helpers';

test.describe('Sprint retros', () => {
  test('cards stay hidden while writing, the host reveals, dots are placed and the action item reaches the team room', async ({ page, browser }) => {
    // A team with one sprint.
    await openApp(page, '/team');
    await page.getByLabel('Team name').fill('Payments');
    await page.getByRole('button', { name: 'Create team room' }).click();
    await page.waitForURL(/\/team\/[0-9a-f-]{36}/);
    const teamUrl = new URL(page.url()).pathname;
    await page.getByLabel('Session name').fill('Sprint 1');
    await page.getByRole('button', { name: 'Start', exact: true }).click();
    await page.waitForURL(/\/game\/[0-9a-f-]{36}/);
    const gameId = page.url().split('/game/')[1].split(/[?#]/)[0];

    // The owner starts the retro from the team room.
    await page.goto(teamUrl);
    await page.getByRole('link', { name: 'Start the retro of Sprint 1' }).click();
    await page.waitForURL(`**/retro/${gameId}`);
    await page.getByRole('button', { name: 'Start the retro' }).click();
    await expect(page.getByRole('heading', { name: 'Went well' })).toBeVisible();

    // A teammate opens the retro link (a guest joins like a game).
    const mate = await (await browser.newContext()).newPage();
    await openApp(mate, `/retro/${gameId}`);
    await expect(mate.getByRole('heading', { name: 'To improve' })).toBeVisible();

    await page.getByLabel('New card in Went well').fill('Pairing worked');
    await page.getByLabel('New card in Went well').press('Enter');
    await mate.getByLabel('New card in To improve').fill('Reviews were slow');
    await mate.getByLabel('New card in To improve').press('Enter');

    // While writing, nobody sees the cards of others.
    await expect(page.getByText('1 cards by others, hidden until the reveal')).toBeVisible();
    await expect(page.getByText('Reviews were slow')).toHaveCount(0);
    await expect(mate.getByText('Pairing worked')).toHaveCount(0);

    await page.getByRole('button', { name: 'Reveal all cards' }).click();
    await expect(mate.getByText('Pairing worked')).toBeVisible();

    await page.getByRole('button', { name: 'Start dot voting' }).click();
    await expect(mate.getByText('3 dots left')).toBeVisible();
    await mate.getByRole('button', { name: 'Add a dot' }).last().click();
    await mate.getByRole('button', { name: 'Add a dot' }).last().click();
    await expect(mate.getByText('1 dots left')).toBeVisible();

    await page.getByRole('button', { name: 'End voting and show results' }).click();
    await expect(page.getByText('2 dots')).toBeVisible();

    // An action item for the whole retro.
    await page.locator('#item-general').fill('Review within a day');
    await page.locator('form.retro-item-add').last().getByRole('button', { name: 'Add' }).click();
    await expect(mate.getByText('Review within a day')).toBeVisible();

    // The team room lists it until it is done.
    await page.goto(teamUrl);
    await expect(page.getByRole('heading', { name: 'Open action items' })).toBeVisible();
    await expect(page.getByText('Review within a day')).toBeVisible();

    await mate.context().close();
  });
});
