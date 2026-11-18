import { expect, Page, test } from '@playwright/test';
import { addTicket, createGame, joinAsSecondPlayer } from './helpers';

/** Moves a ticket with its ⋮ menu (the same path a phone or keyboard user takes). */
async function moveTo(page: Page, ticket: string, card: string): Promise<void> {
  await page.getByRole('button', { name: `Actions for ${ticket}` }).click();
  await page.getByRole('menuitem', { name: card, exact: true }).click();
}

function column(page: Page, card: string) {
  return page.getByRole('region', { name: `Card ${card}` });
}

test.describe('The sorting board', () => {
  test('players sort tickets, a disagreement is flagged and the host applies the board', async ({ page, browser }) => {
    const gameId = await createGame(page, 'Backlog refinement');
    await addTicket(page, 'OPP-10 Search');
    await addTicket(page, 'OPP-11 Export');

    const player = await joinAsSecondPlayer(browser, gameId);

    await page.getByRole('button', { name: 'Room options' }).click();
    await page.getByRole('menuitem', { name: 'Sort the backlog' }).click();
    for (const p of [page, player]) {
      await expect(p.getByRole('heading', { name: 'Sort the backlog' })).toBeVisible();
    }

    await moveTo(page, 'OPP-10 Search', '3');
    await moveTo(page, 'OPP-11 Export', '5');
    // Everyone sees the move.
    await expect(column(player, '3')).toContainText('OPP-10 Search');

    // The other player disagrees about Export.
    await moveTo(player, 'OPP-11 Export', '13');
    for (const p of [page, player]) {
      await expect(column(p, '13')).toContainText('Discuss');
    }

    await page.getByRole('button', { name: 'Apply estimates' }).click();
    const dialog = page.getByRole('dialog');
    await expect(dialog).toContainText('1 tickets get the card');
    await dialog.getByRole('button', { name: 'Apply estimates' }).click();

    await expect(column(page, '3')).toContainText('Estimated: 3');
    await expect(column(page, '13')).not.toContainText('Estimated');

    // Back to the table: the estimate is in the ticket list.
    await page.getByRole('button', { name: 'Back to voting' }).click();
    await expect(player.getByRole('heading', { name: 'Sort the backlog' })).toHaveCount(0);

    await player.context().close();
  });
});
