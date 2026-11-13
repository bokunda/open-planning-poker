/**
 * Cross-platform build script for the marketing website.
 * Generates changelog HTML and copies all assets to dist/.
 */
const { execSync } = require('child_process');
const fs = require('fs');
const path = require('path');

const dist = path.resolve(__dirname, 'dist');

// Clean dist
if (fs.existsSync(dist)) {
  fs.rmSync(dist, { recursive: true });
}
fs.mkdirSync(dist, { recursive: true });

// Generate changelog
console.log('📋 Generating changelog...');
const { english, translated } = require('./generate-changelog');

// Build Tailwind CSS
console.log('🎨 Building Tailwind CSS...');
execSync('npx @tailwindcss/cli -i input.css -o dist/output.css --minify', { stdio: 'inherit' });

// Copy and process changelog.html → changelog/index.html (so /changelog works)
const changelogDir = path.join(dist, 'changelog');
fs.mkdirSync(changelogDir, { recursive: true });
const changelogTemplate = fs.readFileSync(path.resolve(__dirname, 'changelog.html'), 'utf-8');
const changelogContent = fs.readFileSync(path.resolve(__dirname, 'changelog-content.html'), 'utf-8');
const changelogFinal = changelogTemplate.replace('<!-- CHANGELOG_CONTENT -->', changelogContent);

// Home page: "What's new" card and version from the newest release in CHANGELOG.md
const release = english.release;
const latestRelease = { en: english.card, sr: translated.sr.card, de: translated.de.card };
// The stylesheet is small (~7 KB gzipped), so it is inlined into every page: no render-blocking request.
const css = fs.readFileSync(path.join(dist, 'output.css'), 'utf-8');
fs.rmSync(path.join(dist, 'output.css'));
console.log(`  ✓ CSS inlined (${Math.round(css.length / 1024)} KB)`);

// GitHub star count for the header button, fetched at build time (no third-party script on the page).
let stars = '';
try {
  stars = execSync(
    `node -e "fetch('https://api.github.com/repos/bokunda/open-planning-poker',{headers:{'User-Agent':'opp-website-build'}}).then(r=>r.json()).then(j=>console.log(j.stargazers_count ?? ''))"`,
    { timeout: 8000, encoding: 'utf-8' }).trim();
} catch { /* offline build: the button shows without a count */ }
const starsHtml = /^\d+$/.test(stars) ? `<span class="star-count">${stars}</span>` : '';
console.log(`  ✓ GitHub stars: ${stars || 'unknown'}`);

// Shared <head> parts, header and footer (partials/), used by every page.
const partial = name => fs.readFileSync(path.resolve(__dirname, 'partials', name), 'utf-8');
const sitePartials = { head: partial('head.html'), header: partial('header.html'), footer: partial('footer.html') };

const fillRelease = (html, lang = 'en') => html
  .replace('<!-- SITE_HEAD -->', sitePartials.head)
  .replace('<!-- SITE_HEADER -->', sitePartials.header)
  .replace('<!-- SITE_FOOTER -->', sitePartials.footer)
  .replace('<!-- LATEST_RELEASE -->', latestRelease[lang])
  .replace(/<!-- APP_VERSION -->/g, `v${release.version}`)
  .replace(/__APP_VERSION__/g, release.version)
  .replace('<!-- GITHUB_STARS -->', starsHtml)
  .replace(/<link href="\.?\/output\.css" rel="stylesheet">/g, () => `<style>${css}</style>`);
// Every page in English, Serbian and German (localize.js, OPP-24), linked with hreflang.
const { LANGUAGES, GUIDES, PAGES, localize, localizeChrome, finishPage, pageUrl } = require('./localize');
const read = (...parts) => fs.readFileSync(path.resolve(__dirname, ...parts), 'utf-8');
const write = (file, html) => {
  fs.mkdirSync(path.dirname(path.join(dist, file)), { recursive: true });
  fs.writeFileSync(path.join(dist, file), html);
  console.log(`  ✓ ${file}`);
};

// Home: the English page, translated text by text.
const homeEn = read('index.html');
write('index.html', finishPage(fillRelease(homeEn), 'en', ''));
for (const lang of ['sr', 'de']) write(`${lang}/index.html`, localize(fillRelease(homeEn, lang), lang));

// Changelog: one template and one CHANGELOG.md per language.
write('changelog/index.html', finishPage(fillRelease(changelogFinal), 'en', 'changelog/'));
for (const lang of ['sr', 'de']) {
  const page = read('i18n', lang, 'changelog.html').replace('<!-- CHANGELOG_CONTENT -->', translated[lang].content);
  write(`${lang}/changelog/index.html`, localizeChrome(fillRelease(page, lang), lang, 'changelog/'));
}

// Guides: pages/<slug>.html → /<slug>/, translated as whole files in i18n/<lang>/<slug>.html.
for (const slug of GUIDES) {
  write(`${slug}/index.html`, finishPage(fillRelease(read('pages', `${slug}.html`)), 'en', `${slug}/`));
  for (const lang of ['sr', 'de']) {
    const file = path.resolve(__dirname, 'i18n', lang, `${slug}.html`);
    if (!fs.existsSync(file)) throw new Error(`Guide ${slug} has no ${lang} translation: add i18n/${lang}/${slug}.html`);
    write(`${lang}/${slug}/index.html`, localizeChrome(fillRelease(read('i18n', lang, `${slug}.html`), lang), lang, `${slug}/`));
  }
}
console.log(`  ✓ v${release.version}`);

// Sitemap: every localized page in every language, with its alternates. Pages that change with every
// release (home, changelog) use the newest release date; guides use the date in their article metadata.
const guideDate = slug => (read('pages', `${slug}.html`).match(/"dateModified": "([^"]+)"/) || [])[1] || release.date;
const sitemapUrls = PAGES.flatMap(page => Object.keys(LANGUAGES).map(lang => {
  const guide = GUIDES.find(slug => `${slug}/` === page);
  const links = Object.entries(LANGUAGES)
    .map(([code, l]) => `    <xhtml:link rel="alternate" hreflang="${l.htmlLang}" href="${pageUrl(code, page)}"/>`)
    .concat(`    <xhtml:link rel="alternate" hreflang="x-default" href="${pageUrl('en', page)}"/>`);
  const priority = page === '' ? (lang === 'en' ? '1.0' : '0.9') : guide ? '0.8' : '0.6';
  return ['  <url>', `    <loc>${pageUrl(lang, page)}</loc>`, ...links,
    `    <lastmod>${guide ? guideDate(guide) : release.date}</lastmod>`,
    `    <changefreq>${guide ? 'monthly' : 'weekly'}</changefreq>`,
    `    <priority>${priority}</priority>`,
    ...(page === '' && lang === 'en' ? ['<!-- HOME_IMAGES -->'] : []),
    '  </url>'].join('\n');
}));
const sitemap = read('sitemap.xml')
  .replace('<!-- PAGES -->', sitemapUrls.join('\n'))
  .replace(/RELEASE_DATE/g, release.date);
const homeImages = sitemap.match(/<!-- HOME_IMAGES_START -->\r?\n([\s\S]*?)<!-- HOME_IMAGES_END -->\r?\n/);
write('sitemap.xml', sitemap.replace(homeImages[0], '').replace('<!-- HOME_IMAGES -->', homeImages[1].trimEnd()));

fs.writeFileSync(path.join(dist, '404.html'), fillRelease(fs.readFileSync(path.resolve(__dirname, '404.html'), 'utf-8')));

const files = [
  'robots.txt',
  'llms.txt',
  'site.webmanifest',
  'browserconfig.xml'
];

console.log('📁 Copying files...');
for (const file of files) {
  const src = path.resolve(__dirname, file);
  if (fs.existsSync(src)) {
    fs.copyFileSync(src, path.join(dist, file));
    console.log(`  ✓ ${file}`);
  } else {
    console.log(`  ⚠ ${file} not found, skipping`);
  }
}

// Agent discovery manifest (ARD): /.well-known/ai-catalog.json
const wellKnownSrc = path.resolve(__dirname, '.well-known');
if (fs.existsSync(wellKnownSrc)) {
  fs.cpSync(wellKnownSrc, path.join(dist, '.well-known'), { recursive: true });
  console.log('  ✓ .well-known/');
}

// Self-hosted fonts
fs.cpSync(path.resolve(__dirname, 'fonts'), path.join(dist, 'fonts'), { recursive: true });
console.log('  ✓ fonts/');

// Copy images directory
const imagesSrc = path.resolve(__dirname, 'images');
const imagesDest = path.join(dist, 'images');
if (fs.existsSync(imagesSrc)) {
  fs.cpSync(imagesSrc, imagesDest, { recursive: true });
  console.log('  ✓ images/');
}

console.log('✅ Build complete → dist/');
