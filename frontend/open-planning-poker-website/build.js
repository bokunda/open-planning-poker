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
fs.writeFileSync(path.join(changelogDir, 'index.html'), changelogFinal);
console.log('  ✓ changelog/index.html (inlined)');

// Copy remaining files (skip changelog.html since it's now a directory)
const files = [
  'index.html',
  'robots.txt',
  'sitemap.xml',
  'site.webmanifest',
  'browserconfig.xml',
  '404.html'
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

// Copy images directory
const imagesSrc = path.resolve(__dirname, 'images');
const imagesDest = path.join(dist, 'images');
if (fs.existsSync(imagesSrc)) {
  fs.cpSync(imagesSrc, imagesDest, { recursive: true });
  console.log('  ✓ images/');
}

console.log('✅ Build complete → dist/');
