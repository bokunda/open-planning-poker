/**
 * Localized pages (OPP-24): the home page, the guides and the changelog in Serbian (Latin script, /sr/) and
 * German (/de/).
 *  - The home page is built from the English page with the texts in i18n/<lang>.json. Only whole text nodes,
 *    a few attributes and JSON-LD strings are replaced, so short words never change inside other text.
 *  - Guides and the changelog page are translated as whole files in i18n/<lang>/; only their header and footer
 *    (the shared partials) are translated with i18n/<lang>.json.
 * Every page gets hreflang links to its other languages, and links between pages stay in the page's language.
 */
const fs = require('fs');
const path = require('path');

const SITE = 'https://openplanningpoker.com';

const LANGUAGES = {
  en: { path: '/', htmlLang: 'en', ogLocale: 'en_US' },
  sr: { path: '/sr/', htmlLang: 'sr-Latn', ogLocale: 'sr_RS' },
  de: { path: '/de/', htmlLang: 'de', ogLocale: 'de_DE' },
};

/** Guide slugs: pages/<slug>.html in English, i18n/<lang>/<slug>.html translated. */
const GUIDES = fs.readdirSync(path.resolve(__dirname, 'pages')).filter(f => f.endsWith('.html')).map(f => f.replace(/\.html$/, ''));

/** Pages that exist in every language, relative to the language root ('' is the home page). */
const PAGES = ['', ...GUIDES.map(slug => `${slug}/`), 'changelog/'];

const TRANSLATED_ATTRIBUTES = ['alt', 'aria-label', 'title', 'content', 'placeholder'];

const pageUrl = (lang, page) => `${SITE}${LANGUAGES[lang].path}${page}`;

/** hreflang links for one page in all languages. */
function alternates(page) {
  return Object.entries(LANGUAGES)
    .map(([code, l]) => `<link rel="alternate" hreflang="${l.htmlLang}" href="${pageUrl(code, page)}">`)
    .concat(`<link rel="alternate" hreflang="x-default" href="${pageUrl('en', page)}">`)
    .join('\n    ');
}

/** Replaces any hreflang links in the head with the full set, right after the canonical link. */
function withAlternates(html, page = '') {
  return html
    .replace(/\s*<link rel="alternate" hreflang="[^"]+" href="[^"]+">/g, '')
    .replace(/(<link rel="canonical" href="[^"]+">)/, `$1\n    ${alternates(page)}`);
}

const dictionary = lang => JSON.parse(fs.readFileSync(path.resolve(__dirname, 'i18n', `${lang}.json`), 'utf-8'));

/** Translates whole text nodes, attributes and JSON-LD strings found in the dictionary. */
function translateTexts(html, dict, used) {
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

  return html.replace(/\u0000(\d+)\u0000/g, (_, i) => blocks[Number(i)]);
}

/**
 * Language attributes, canonical URL, hreflang links, links to other pages in the same language, and the
 * footer's language switcher pointing to this page in each language. Used for every page, English included.
 */
function finishPage(html, lang, page) {
  const l = LANGUAGES[lang];
  html = html
    .replace(/<html lang="[^"]+"/, `<html lang="${l.htmlLang}"`)
    .replace(`<link rel="canonical" href="${SITE}/${page}">`, `<link rel="canonical" href="${pageUrl(lang, page)}">`)
    .replace(`<meta property="og:url" content="${SITE}/${page}">`, `<meta property="og:url" content="${pageUrl(lang, page)}">`)
    .replace(/<meta property="og:locale" content="[^"]+">/, `<meta property="og:locale" content="${l.ogLocale}">`);

  if (lang !== 'en') {
    html = html
      .replace(/href="\/#/g, `href="${l.path}#`)
      .replace(/href="\/"/g, `href="${l.path}"`)
      .replace(/href="\/([a-z-]+\/)"/g, (match, target) => (PAGES.includes(target) ? `href="${l.path}${target}"` : match));
  }

  // Language switcher: <a href="…" hreflang="sr-Latn" …> goes to this page in that language.
  html = html.replace(/href="[^"]*"( hreflang="(en|sr-Latn|de)")/g, (_, attr, htmlLang) => {
    const code = Object.keys(LANGUAGES).find(c => LANGUAGES[c].htmlLang === htmlLang);
    return `href="${LANGUAGES[code].path}${page}"${attr}`;
  });
  return withAlternates(html, page);
}

/** The home page in another language, from the English page; fails when the dictionary has unused texts. */
function localize(html, lang) {
  const dict = dictionary(lang);
  const used = new Set();
  html = finishPage(translateTexts(html, dict, used), lang, '');
  const unused = Object.keys(dict).filter((key) => !used.has(key));
  if (unused.length) {
    throw new Error(`i18n/${lang}.json has texts that are not on the page any more:\n  ${unused.join('\n  ')}`);
  }
  return html;
}

/** A page that is already translated as a whole file: only the shared header and footer are translated here. */
function localizeChrome(html, lang, page) {
  return finishPage(translateTexts(html, dictionary(lang), new Set()), lang, page);
}

module.exports = { LANGUAGES, GUIDES, PAGES, SITE, localize, localizeChrome, finishPage, withAlternates, pageUrl };
