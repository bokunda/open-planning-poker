import { DECK_PRESETS } from './deck-presets';

/**
 * Where a visitor came from on the website (OPP-181): links on the website carry ?ref=<page>, and the first room the
 * visitor creates reports that page. Only the page name is kept, for this browser tab; no cookie, nothing personal.
 */
const KEY = 'opp-ref';
const VALID = /^[a-z0-9-]{1,40}$/;

export function rememberRef(value: string | null | undefined): void {
  if (!value || !VALID.test(value)) return;
  try { sessionStorage.setItem(KEY, value); } catch { /* storage blocked: nothing to report */ }
}

export function siteRef(): string | null {
  try {
    const value = sessionStorage.getItem(KEY);
    return value && VALID.test(value) ? value : null;
  } catch {
    return null;
  }
}

/** Reported once: the next room of the tab counts as created from the app itself. */
export function clearRef(): void {
  try { sessionStorage.removeItem(KEY); } catch { /* ignore */ }
}

/** "Use this deck" on the website links ?deck=<name>: the preset's cards, or null for an unknown name. */
export function deckFromName(name: string | null | undefined): string | null {
  const key = name ? `deck.${name}` : '';
  return DECK_PRESETS.find(p => p.key === key)?.value ?? null;
}
