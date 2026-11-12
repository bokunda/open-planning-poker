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
require('./generate-changelog');

// Build Tailwind CSS
console.log('🎨 Building Tailwind CSS...');
execSync('npx tailwindcss -i input.css -o dist/output.css --minify', { stdio: 'inherit' });

// Copy and process changelog.html → changelog/index.html (so /changelog works)
const changelogDir = path.join(dist, 'changelog');
fs.mkdirSync(changelogDir, { recursive: true });
const changelogTemplate = fs.readFileSync(path.resolve(__dirname, 'changelog.html'), 'utf-8');
const changelogContent = fs.readFileSync(path.resolve(__dirname, 'changelog-content.html'), 'utf-8');
const changelogFinal = changelogTemplate.replace('<!-- CHANGELOG_CONTENT -->', changelogContent);

// Home page: "What's new" card and version from the newest release in CHANGELOG.md
const release = JSON.parse(fs.readFileSync(path.resolve(__dirname, 'release.json'), 'utf-8'));
const latestRelease = fs.readFileSync(path.resolve(__dirname, 'latest-release.html'), 'utf-8');
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

const fillRelease = html => html
  .replace('<!-- SITE_HEAD -->', sitePartials.head)
  .replace('<!-- SITE_HEADER -->', sitePartials.header)
  .replace('<!-- SITE_FOOTER -->', sitePartials.footer)
  .replace('<!-- LATEST_RELEASE -->', latestRelease)
  .replace(/<!-- APP_VERSION -->/g, `v${release.version}`)
  .replace(/__APP_VERSION__/g, release.version)
  .replace('<!-- GITHUB_STARS -->', starsHtml)
  .replace(/<link href="\.?\/output\.css" rel="stylesheet">/g, () => `<style>${css}</style>`);
// Home page in English, Serbian and German (localize.js), linked with hreflang.
const { localize, withAlternates } = require('./localize');
const home = withAlternates(fillRelease(fs.readFileSync(path.resolve(__dirname, 'index.html'), 'utf-8')));
fs.writeFileSync(path.join(dist, 'index.html'), home);
for (const lang of ['sr', 'de']) {
  fs.mkdirSync(path.join(dist, lang), { recursive: true });
  fs.writeFileSync(path.join(dist, lang, 'index.html'), localize(home, lang));
  console.log(`  ✓ ${lang}/index.html`);
}
fs.writeFileSync(path.join(changelogDir, 'index.html'), fillRelease(changelogFinal));
console.log(`  ✓ index.html, changelog/index.html (v${release.version})`);

// Sitemap: pages that change with every release (home, changelog, app) use the newest release date
const sitemap = fs.readFileSync(path.resolve(__dirname, 'sitemap.xml'), 'utf-8')
  .replace(/RELEASE_DATE/g, release.date);
fs.writeFileSync(path.join(dist, 'sitemap.xml'), sitemap);
console.log('  ✓ sitemap.xml');

// Copy remaining files (skip changelog.html since it's now a directory)
// Guide pages: pages/<slug>.html → /<slug>/
const pagesDir = path.resolve(__dirname, 'pages');
for (const file of fs.readdirSync(pagesDir).filter(f => f.endsWith('.html'))) {
  const slug = file.replace(/\.html$/, '');
  fs.mkdirSync(path.join(dist, slug), { recursive: true });
  fs.writeFileSync(path.join(dist, slug, 'index.html'), fillRelease(fs.readFileSync(path.join(pagesDir, file), 'utf-8')));
  console.log(`  ✓ ${slug}/index.html`);
}

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
