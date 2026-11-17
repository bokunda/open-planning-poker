/** Value of `?embed=` in the Microsoft Teams tab URL (OPP-23). */
export const EMBED_TEAMS = 'teams';

const KEY = 'opp-embed';

/**
 * True when the app runs inside the Teams tab: the tab opens `/game/<id>?embed=teams`. The flag is kept for the
 * browser tab's session, so moving to a ticket (a URL without the query) keeps the compact layout.
 * Browser only; call it after hydration.
 */
export function isEmbedded(): boolean {
  try {
    if (new URLSearchParams(window.location.search).get('embed') === EMBED_TEAMS) {
      sessionStorage.setItem(KEY, EMBED_TEAMS);
      return true;
    }
    return sessionStorage.getItem(KEY) === EMBED_TEAMS;
  } catch {
    return false;
  }
}
