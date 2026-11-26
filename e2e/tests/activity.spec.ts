import { expect, test } from '@playwright/test';
import { addTicket, createGame, expectNoA11yViolations, joinAsSecondPlayer, vote } from './helpers';

test.describe('The activity log (GitHub #20)', () => {
  test('the host sees who did what in the room; players do not see the panel', async ({ page, browser }) => {
    const gameId = await createGame(page, 'Audit trail');
    await addTicket(page, 'OPP-20 Audit logs');

    const player = await joinAsSecondPlayer(browser, gameId);
    await vote(page, '3');
    await vote(player, '5');
    await page.getByRole('button', { name: 'Reveal cards' }).click();
    await expect(page.locator('app-vote-results')).toBeVisible();

    // The panel starts closed and loads when the host opens it.
    await page.getByRole('button', { name: 'Show activity' }).click();
    const entries = page.getByTestId('activity-panel').getByTestId('activity-entry');
    await expect(entries.first()).toContainText('revealed the votes on OPP-20 Audit logs');
    await expect(entries.last()).toContainText('added the ticket OPP-20 Audit logs');
    // Votes are never part of the log.
    await expect(entries).toHaveCount(2);

    // A new round shows up while the panel is open.
    await page.getByRole('button', { name: 'Vote again' }).click();
    await expect(entries).toHaveCount(3);
    await expect(entries.first()).toContainText('cleared the votes on OPP-20 Audit logs');

    await expectNoA11yViolations(page);

    await expect(player.getByRole('button', { name: 'Show activity' })).toHaveCount(0);
    await player.context().close();
  });
});
