/** Game id from a pasted link (`.../game/<id>...`) or a plain id; other input is returned trimmed. */
export function extractGameId(input: string): string {
  const trimmed = input.trim();
  // Full URL with UUID (with dashes): https://app.openplanningpoker.com/game/019f6820-52fd-7ef0-9c6c-6bfae207ae02/
  const urlMatch = trimmed.match(/\/game\/([a-f0-9-]{36})/i);
  if (urlMatch) return urlMatch[1];
  // Plain UUID with dashes: 019f6820-52fd-7ef0-9c6c-6bfae207ae02
  const uuidMatch = trimmed.match(/^([a-f0-9]{8}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{12})$/i);
  if (uuidMatch) return uuidMatch[1];
  // Return as-is (validator will reject if not 36 chars)
  return trimmed;
}
