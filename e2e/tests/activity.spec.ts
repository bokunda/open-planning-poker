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
    const panel = page.getByTestId('activity-panel');
    const entries = panel.getByTestId('activity-entry');
    const entry = (action: string) => panel.locator(`[data-testid="activity-entry"][data-action="${action}"]`);

    await expect(entries.first()).toContainText('revealed the votes on OPP-20 Audit logs');
    await expect(entry('ticket.created')).toContainText('added the ticket OPP-20 Audit logs');
    // Creating the game with the default deck is not a deck change, and votes are never part of the log.
    await expect(entry('settings.deck')).toHaveCount(0);
    await expect(entries).toHaveCount(2);

    // A new round shows up while the panel is open.
    await page.getByRole('button', { name: 'Vote again' }).click();
    await expect(entry('votes.reset')).toHaveCount(1);
    await expect(entries.first()).toContainText('cleared the votes on OPP-20 Audit logs');

    await expectNoA11yViolations(page);

    await expect(player.getByRole('button', { name: 'Show activity' })).toHaveCount(0);
    await player.context().close();
  });
});
