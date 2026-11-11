/**
 * Generates the website parts that come from CHANGELOG.md, using marked:
 *  - changelog-content.html: the whole changelog for /changelog/
 *  - latest-release.html: the "What's new" card on the home page (newest release only)
 *  - release.json: { version, date } of the newest release, for the footer and the sitemap
 * Expects CHANGELOG.md in the same directory (copied by CI) or at the repo root (local dev).
 */
const { marked } = require('marked');
const fs = require('fs');
const path = require('path');

let changelogPath = path.resolve(__dirname, 'CHANGELOG.md');
if (!fs.existsSync(changelogPath)) {
  changelogPath = path.resolve(__dirname, '..', '..', 'CHANGELOG.md');
  console.log('ℹ Using repo root CHANGELOG.md');
}
const md = fs.readFileSync(changelogPath, 'utf-8');

// Full changelog page
// The page has its own title and intro, so start at the first release and drop an empty Unreleased section.
const releasesMd = md.slice(md.search(/^## /m)).replace(/^## \[Unreleased\]\s*(?=^## )/m, '');
fs.writeFileSync(path.resolve(__dirname, 'changelog-content.html'), `<div class="changelog-content">${marked.parse(releasesMd)}</div>`);
console.log('✅ changelog-content.html');

// Newest release: the first "## [x.y.z] — date" heading (Unreleased is skipped).
const releaseRe = /^## \[(\d+\.\d+\.\d+)\]\s*[—-]\s*([^\n]+)\n([\s\S]*?)(?=^## \[|(?![\s\S]))/m;
const match = md.match(releaseRe);
if (!match) throw new Error('No released version found in CHANGELOG.md');
const [, version, date, body] = match;

// Use the "### Highlights" list when there is one, otherwise the first bullets of the release.
const highlights = body.match(/^### Highlights\n([\s\S]*?)(?=^### |(?![\s\S]))/m);
const bullets = (highlights ? highlights[1] : body)
  .split('\n')
  .filter(line => line.startsWith('- '))
  .slice(0, 6)
  .map(line => marked.parseInline(line.slice(2)));
const intro = body.split('\n').find(line => line.trim() && !line.startsWith('#') && !line.startsWith('- ')) || '';

const prettyDate = (() => {
  const d = new Date(date.trim());
  return isNaN(d) ? date.trim() : d.toLocaleDateString('en-US', { year: 'numeric', month: 'long', day: 'numeric', timeZone: 'UTC' });
})();

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
fs.writeFileSync(path.resolve(__dirname, 'latest-release.html'), card);
fs.writeFileSync(path.resolve(__dirname, 'release.json'), JSON.stringify({ version, date: date.trim() }));
console.log(`✅ latest-release.html (v${version}, ${date.trim()})`);

// The app's footer reads its version from package.json; warn when the two disagree.
const appPkg = path.resolve(__dirname, '..', 'open-planning-poker-web-app', 'package.json');
if (fs.existsSync(appPkg)) {
  const appVersion = JSON.parse(fs.readFileSync(appPkg, 'utf-8')).version;
  if (appVersion !== version) {
    console.warn(`⚠ App package.json is ${appVersion} but the newest CHANGELOG release is ${version}`);
  }
}
