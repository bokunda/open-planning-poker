import { expect, test } from '@playwright/test';
import { addTicket, createGame, expectNoA11yViolations, joinAsSecondPlayer, vote } from './helpers';

test.describe('A planning session', () => {
  test('two players vote, reveal and vote again', async ({ page, browser }) => {
    const gameId = await createGame(page);
    await addTicket(page, 'OPP-1 Login page');
    await expect(page.getByRole('heading', { name: /OPP-1 Login page/ })).toBeVisible();

    const player = await joinAsSecondPlayer(browser, gameId);
    await expect(player.getByRole('heading', { name: /OPP-1 Login page/ })).toBeVisible();

    await vote(page, '3');
    await vote(player, '8');

    // Before the reveal the host only sees that the other player voted, not the card.
    await expect(page.getByRole('button', { name: 'Reveal cards' })).toBeVisible();
    await expect(page.locator('app-vote-results')).toHaveCount(0);

    await page.getByRole('button', { name: 'Reveal cards' }).click();
    for (const p of [page, player]) {
      const results = p.locator('app-vote-results');
      await expect(results).toBeVisible();
      await expect(results).toContainText('3');
      await expect(results).toContainText('8');
    }

    await page.getByRole('button', { name: 'Vote again' }).click();
    await expect(page.locator('app-vote-results')).toHaveCount(0);
    await expect(player.locator('app-vote-results')).toHaveCount(0);
    await expect(player.getByRole('button', { name: 'Vote 5', exact: true })).toBeEnabled();

    await player.context().close();
  });

  test('the host imports tickets from a pasted list', async ({ page }) => {
    await createGame(page);
    await page.getByRole('button', { name: /Import/ }).first().click();
    const dialog = page.getByRole('dialog');
    await dialog.getByLabel('Paste tickets').fill('OPP-10 Search\nOPP-11 Filters | By date and owner\nOPP-12 Saved views');
    await dialog.getByRole('button', { name: 'Import 3 tickets' }).click();
    await expect(dialog).toBeHidden();

    for (const name of ['OPP-10 Search', 'OPP-11 Filters', 'OPP-12 Saved views']) {
      await expect(page.getByText(name).first()).toBeVisible();
    }
  });

  test('the shared timer runs for everyone and the host can stop it', async ({ page, browser }) => {
    const gameId = await createGame(page);
    await addTicket(page, 'OPP-2 Timer');
    const player = await joinAsSecondPlayer(browser, gameId);

    const hostTimer = page.getByRole('group', { name: 'Voting timer' });
    await hostTimer.getByRole('button', { name: 'Start' }).click();
    await expect(hostTimer.getByRole('button', { name: 'Stop' })).toBeVisible();
    await expect(player.getByRole('group', { name: 'Voting timer' })).toBeVisible();

    await hostTimer.getByRole('button', { name: 'Stop' }).click();
    await expect(hostTimer.getByRole('button', { name: 'Start' })).toBeVisible();
    await expect(player.getByRole('group', { name: 'Voting timer' })).toBeHidden();

    await player.context().close();
  });

  test('the host exports the results as a PDF', async ({ page }) => {
    await createGame(page, 'Export check');
    await addTicket(page, 'OPP-3 Report');
    await vote(page, '5');
    await page.getByRole('button', { name: 'Reveal cards' }).click();
    await expect(page.locator('app-vote-results')).toBeVisible();

    const download = page.waitForEvent('download');
    await page.getByRole('button', { name: 'Export results as PDF' }).click();
    const file = await download;
    expect(file.suggestedFilename()).toMatch(/\.pdf$/);
  });

  test('the game room has no WCAG 2 A/AA violations', async ({ page }) => {
    await createGame(page, 'Accessibility check');
    await addTicket(page, 'OPP-4 Accessible room');
    await expectNoA11yViolations(page);
  });
});
