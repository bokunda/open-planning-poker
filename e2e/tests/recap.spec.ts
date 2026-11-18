import { expect, test } from '@playwright/test';
import { addTicket, createGame, joinAsSecondPlayer, vote } from './helpers';

test.describe('Shareable recaps and the room look', () => {
  test('consensus is celebrated, the host shares a recap that opens without the room, and a theme reaches everyone', async ({ page, browser }) => {
    const gameId = await createGame(page, 'Sprint 7 planning');
    await addTicket(page, 'OPP-20 Login page');
    const player = await joinAsSecondPlayer(browser, gameId);

    await vote(page, '5');
    await vote(player, '5');
    await page.getByRole('button', { name: 'Reveal cards' }).click();
    for (const p of [page, player]) {
      await expect(p.getByText('Consensus! Everyone picked the same card.')).toBeAttached();
    }

    // The host picks a table theme; the other player sees it too.
    await page.getByRole('button', { name: 'Room options' }).click();
    await page.getByRole('menuitem', { name: /^Table:/ }).click();
    await page.getByRole('menuitemradio', { name: 'Ocean' }).click();
    await expect(player.locator('.game-dashboard')).toHaveClass(/room-theme-ocean/);

    // Share the recap.
    await page.getByRole('button', { name: 'Room options' }).click();
    await page.getByRole('menuitem', { name: 'Share the recap' }).click();
    const dialog = page.getByRole('dialog');
    await dialog.getByRole('button', { name: 'Share' }).click();
    const link = dialog.getByLabel('Recap link');
    await expect(link).toHaveValue(/\/recap\/[A-Za-z0-9_-]{22}$/);
    const url = await link.inputValue();

    // Someone who was not in the room opens it.
    const outsider = await (await browser.newContext()).newPage();
    await outsider.goto(new URL(url).pathname);
    await expect(outsider.getByRole('heading', { level: 1, name: 'Sprint 7 planning' })).toBeVisible();
    await expect(outsider.getByText('OPP-20 Login page')).toBeVisible();
    await expect(outsider.getByRole('link', { name: 'Start your own room' })).toBeVisible();

    // Stop sharing: the link no longer works.
    await dialog.getByRole('button', { name: 'Stop sharing' }).click();
    await expect(dialog.getByRole('button', { name: 'Share' })).toBeVisible();
    await outsider.reload();
    await expect(outsider.getByRole('heading', { name: 'This recap is not shared' })).toBeVisible();

    await outsider.context().close();
    await player.context().close();
  });
});
