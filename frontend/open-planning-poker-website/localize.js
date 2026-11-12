/**
 * Localized home pages (OPP-24): /sr/ (Serbian, Latin script) and /de/ (German), built from the English
 * page with the texts in i18n/<lang>.json. Only whole text nodes, a few attributes and JSON-LD strings are
 * replaced, so short words never change inside other text. Guides and the changelog stay in English.
 */
const fs = require('fs');
const path = require('path');

const SITE = 'https://openplanningpoker.com';

const LANGUAGES = {
  en: { path: '/', htmlLang: 'en', ogLocale: 'en_US' },
  sr: { path: '/sr/', htmlLang: 'sr-Latn', ogLocale: 'sr_RS' },
  de: { path: '/de/', htmlLang: 'de', ogLocale: 'de_DE' },
};

const TRANSLATED_ATTRIBUTES = ['alt', 'aria-label', 'title', 'content', 'placeholder'];

/** hreflang links for the three home pages; replaces the English-only pair in index.html. */
function alternates() {
  return Object.entries(LANGUAGES)
    .map(([code, l]) => `<link rel="alternate" hreflang="${l.htmlLang}" href="${SITE}${l.path}">`)
    .concat(`<link rel="alternate" hreflang="x-default" href="${SITE}/">`)
    .join('\n    ');
}

function withAlternates(html) {
  return html.replace(
    /<link rel="alternate" hreflang="en" href="https:\/\/openplanningpoker\.com\/">\s*<link rel="alternate" hreflang="x-default" href="https:\/\/openplanningpoker\.com\/">/,
    alternates());
}

function localize(html, lang) {
  const dict = JSON.parse(fs.readFileSync(path.resolve(__dirname, 'i18n', `${lang}.json`), 'utf-8'));
  const used = new Set();
  const t = (text) => {
    const key = text.trim();
    if (!(key in dict)) return null;
    used.add(key);
    return text.replace(key, dict[key]);
  };

  // Keep styles and scripts out of the text replacement; JSON-LD is translated value by value.
  const blocks = [];
  html = html.replace(/<(style|script)\b[^>]*>[\s\S]*?<\/\1>/g, (block) => {
    if (block.startsWith('<script type="application/ld+json">')) {
      const json = block.slice(block.indexOf('>') + 1, block.lastIndexOf('</script>'));
      const data = JSON.parse(json, (key, value) => (typeof value === 'string' && key !== '@type' && key !== '@context' ? (t(value) ?? value) : value));
      block = `<script type="application/ld+json">${JSON.stringify(data)}</script>`;
    }
    blocks.push(block);
    return `\u0000${blocks.length - 1}\u0000`;
  });

  html = html.replace(/>([^<>\u0000]+)</g, (match, text) => {
    const translated = t(text);
    return translated === null ? match : `>${translated}<`;
  });
  const attrs = new RegExp(`\\b(${TRANSLATED_ATTRIBUTES.join('|')})="([^"]+)"`, 'g');
  html = html.replace(attrs, (match, name, value) => {
    const translated = t(value);
    return translated === null ? match : `${name}="${translated.replace(/"/g, '&quot;')}"`;
  });

  html = html.replace(/\u0000(\d+)\u0000/g, (_, i) => blocks[Number(i)]);

  const l = LANGUAGES[lang];
  html = html
    .replace('<html lang="en"', `<html lang="${l.htmlLang}"`)
    .replace(`<link rel="canonical" href="${SITE}/">`, `<link rel="canonical" href="${SITE}${l.path}">`)
    .replace(`<meta property="og:url" content="${SITE}/">`, `<meta property="og:url" content="${SITE}${l.path}">`)
    .replace('<meta property="og:locale" content="en_US">', `<meta property="og:locale" content="${l.ogLocale}">`)
    // Links within the home page stay on the localized page.
    .replace(/href="\/#/g, `href="${l.path}#`)
    .replace(/href="\/"/g, `href="${l.path}"`);

  const unused = Object.keys(dict).filter((key) => !used.has(key));
  if (unused.length) {
    throw new Error(`i18n/${lang}.json has texts that are not on the page any more:\n  ${unused.join('\n  ')}`);
  }
  return html;
}

module.exports = { LANGUAGES, localize, withAlternates, SITE };
