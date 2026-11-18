import type { Language } from '../i18n/language';

/*
 * Link previews of game invites (OPP-171): when a game link is pasted into Slack, Teams or an email, the preview shows
 * the room name instead of the generic page title. Runs in server.ts after rendering; never fails the page.
 */

const INVITE_TEXT: Record<Language, { title: (name: string) => string; description: string }> = {
  en: { title: name => `Join “${name}” — planning poker`, description: 'You are invited to estimate together. Open the link, pick a name and vote. No sign-up needed.' },
  sr: { title: name => `Pridruži se: „${name}” — planning poker`, description: 'Pozvani ste da zajedno procenite tikete. Otvorite link, upišite ime i glasajte. Bez registracije.' },
  de: { title: name => `„${name}“ beitreten — Planning Poker`, description: 'Sie sind eingeladen, gemeinsam zu schätzen. Link öffnen, Namen wählen und abstimmen. Ohne Anmeldung.' },
};

export function escapeHtml(value: string): string {
  return value.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;');
}

/** Sets the content of one meta tag (matched by property or name) in rendered HTML; other tags stay untouched. */
export function setMetaContent(html: string, key: string, content: string): string {
  const pattern = new RegExp(`(<meta\\s+(?:property|name)="${key.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')}"\\s+content=")[^"]*(")`);
  return html.replace(pattern, (_, start: string, end: string) => `${start}${escapeHtml(content)}${end}`);
}

/** Puts the room name into the title and the Open Graph / Twitter tags of a rendered game page. */
export function withInvitePreview(html: string, gameName: string, language: Language): string {
  const text = INVITE_TEXT[language] ?? INVITE_TEXT.en;
  const name = gameName.length > 80 ? `${gameName.slice(0, 79)}…` : gameName;
  const title = text.title(name);
  let out = html.replace(/<title>[^<]*<\/title>/, `<title>${escapeHtml(title)}</title>`);
  for (const key of ['og:title', 'twitter:title']) out = setMetaContent(out, key, title);
  for (const key of ['og:description', 'twitter:description', 'description']) out = setMetaContent(out, key, text.description);
  return out;
}

/** Name of a game, read from the GraphQL gateway with a short timeout; null when it is not found or slow. */
export async function fetchGameName(gatewayUrl: string, gameId: string, timeoutMs = 1500): Promise<string | null> {
  try {
    const response = await fetch(gatewayUrl, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ query: 'query($id: UUID!) { game(id: $id) { ... on Game { name } } }', variables: { id: gameId } }),
      signal: AbortSignal.timeout(timeoutMs),
    });
    if (!response.ok) return null;
    const body = await response.json() as { data?: { game?: { name?: string } } };
    const name = body.data?.game?.name?.trim();
    return name ? name : null;
  } catch {
    return null;
  }
}
