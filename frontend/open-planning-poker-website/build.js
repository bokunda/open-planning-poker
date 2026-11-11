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
// Fingerprint the stylesheet: it is cached for a year, so a new build must use a new file name.
const cssHash = require('crypto').createHash('sha256').update(fs.readFileSync(path.join(dist, 'output.css'))).digest('hex').slice(0, 10);
const cssFile = `output.${cssHash}.css`;
fs.renameSync(path.join(dist, 'output.css'), path.join(dist, cssFile));
console.log(`  ✓ ${cssFile}`);

const fillRelease = html => html
  .replace(/(href=")\.?\/output\.css"/g, `$1/${cssFile}"`)
  .replace('<!-- LATEST_RELEASE -->', latestRelease)
  .replace(/<!-- APP_VERSION -->/g, `v${release.version}`)
  .replace(/__APP_VERSION__/g, release.version);
fs.writeFileSync(path.join(dist, 'index.html'), fillRelease(fs.readFileSync(path.resolve(__dirname, 'index.html'), 'utf-8')));
fs.writeFileSync(path.join(changelogDir, 'index.html'), fillRelease(changelogFinal));
console.log(`  ✓ index.html, changelog/index.html (v${release.version})`);

// Sitemap: last modified = the newest release date
const sitemap = fs.readFileSync(path.resolve(__dirname, 'sitemap.xml'), 'utf-8')
  .replace(/<lastmod>[^<]*<\/lastmod>/g, `<lastmod>${release.date}</lastmod>`);
fs.writeFileSync(path.join(dist, 'sitemap.xml'), sitemap);
console.log('  ✓ sitemap.xml');

// Copy remaining files (skip changelog.html since it's now a directory)
fs.writeFileSync(path.join(dist, '404.html'), fillRelease(fs.readFileSync(path.resolve(__dirname, '404.html'), 'utf-8')));

const files = [
  'robots.txt',
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
