/**
 * Generates the website parts that come from the changelog, using marked:
 *  - changelog-content.html: the whole changelog for /changelog/
 *  - latest-release.html: the "What's new" card on the home page (newest release only)
 *  - release.json: { version, date } of the newest release, for the footer and the sitemap
 * The English changelog is CHANGELOG.md (copied next to this file by CI, or at the repo root for local dev).
 * The Serbian and German ones are i18n/<lang>/CHANGELOG.md (OPP-24); they must list the same releases.
 */
const { marked } = require('marked');
const fs = require('fs');
const path = require('path');

const LOCALES = { en: 'en-US', sr: 'sr-Latn-RS', de: 'de-DE' };

function changelogPath(lang) {
  if (lang !== 'en') return path.resolve(__dirname, 'i18n', lang, 'CHANGELOG.md');
  const local = path.resolve(__dirname, 'CHANGELOG.md');
  return fs.existsSync(local) ? local : path.resolve(__dirname, '..', '..', 'CHANGELOG.md');
}

/** Released versions in a changelog, newest first ("Unreleased" is not one). */
const versionsOf = md => [...md.matchAll(/^## \[(\d+\.\d+\.\d+)\]/gm)].map(m => m[1]);

/** The changelog page content, the "What's new" card and the newest release of one language. */
function generate(lang) {
  const md = fs.readFileSync(changelogPath(lang), 'utf-8');

  // The page has its own title and intro, so start at the first release and drop an empty Unreleased section.
  const releasesMd = md.slice(md.search(/^## /m)).replace(/^## \[(Unreleased|Neobjavljeno|Unveröffentlicht)\]\s*(?=^## )/m, '');
  const content = `<div class="changelog-content">${marked.parse(releasesMd)}</div>`;

  // Newest release: the first "## [x.y.z] — date" heading (Unreleased is skipped).
  const releaseRe = /^## \[(\d+\.\d+\.\d+)\]\s*[—-]\s*([^\n]+)\n([\s\S]*?)(?=^## \[|(?![\s\S]))/m;
  const match = md.match(releaseRe);
  if (!match) throw new Error(`No released version found in ${changelogPath(lang)}`);
  const [, version, date, body] = match;

  // Use the "### Highlights" list when there is one, otherwise the first bullets of the release.
  const highlights = body.match(/^### (?:Highlights|Najvažnije)\n([\s\S]*?)(?=^### |(?![\s\S]))/m);
  const bullets = (highlights ? highlights[1] : body)
    .split('\n')
    .filter(line => line.startsWith('- '))
    .slice(0, 6)
    .map(line => marked.parseInline(line.slice(2)));
  const intro = body.split('\n').find(line => line.trim() && !line.startsWith('#') && !line.startsWith('- ')) || '';

  const d = new Date(date.trim());
  const prettyDate = isNaN(d) ? date.trim() : d.toLocaleDateString(LOCALES[lang], { year: 'numeric', month: 'long', day: 'numeric', timeZone: 'UTC' });

  const card = `<article class="whats-new-card">
  <div class="whats-new-meta">
    <span class="whats-new-version">v${version}</span>
    <time datetime="${date.trim()}">${prettyDate}</time>
  </div>
  ${intro ? `<p class="whats-new-intro">${marked.parseInline(intro)}</p>` : ''}
  <ul class="whats-new-list">
${bullets.map(b => `    <li>${b}</li>`).join('\n')}
  </ul>
</article>`;
  return { content, card, release: { version, date: date.trim() }, versions: versionsOf(md) };
}

const english = generate('en');
fs.writeFileSync(path.resolve(__dirname, 'changelog-content.html'), english.content);
fs.writeFileSync(path.resolve(__dirname, 'latest-release.html'), english.card);
fs.writeFileSync(path.resolve(__dirname, 'release.json'), JSON.stringify(english.release));
console.log(`✅ changelog-content.html, latest-release.html (v${english.release.version}, ${english.release.date})`);

// Translated changelogs must have exactly the English releases, so a release is never missing in one language.
const translated = {};
for (const lang of ['sr', 'de']) {
  translated[lang] = generate(lang);
  const missing = english.versions.filter(v => !translated[lang].versions.includes(v));
  const extra = translated[lang].versions.filter(v => !english.versions.includes(v));
  if (missing.length || extra.length) {
    throw new Error(`i18n/${lang}/CHANGELOG.md does not match CHANGELOG.md.`
      + (missing.length ? ` Missing: ${missing.join(', ')}.` : '')
      + (extra.length ? ` Not in CHANGELOG.md: ${extra.join(', ')}.` : ''));
  }
  console.log(`✅ ${lang} changelog (${translated[lang].versions.length} releases)`);
}

// The app's footer reads its version from package.json; warn when the two disagree.
const appPkg = path.resolve(__dirname, '..', 'open-planning-poker-web-app', 'package.json');
if (fs.existsSync(appPkg)) {
  const appVersion = JSON.parse(fs.readFileSync(appPkg, 'utf-8')).version;
  if (appVersion !== english.release.version) {
    console.warn(`⚠ App package.json is ${appVersion} but the newest CHANGELOG release is ${english.release.version}`);
  }
}

module.exports = { english, translated };
