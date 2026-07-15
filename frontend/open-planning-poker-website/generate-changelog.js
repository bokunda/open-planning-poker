/**
 * Generates changelog-content.html partial from CHANGELOG.md using marked.
 * Expects CHANGELOG.md in the same directory (copied by CI or developer).
 * Uses default renderer + scoped Tailwind CSS for styling.
 */
const { marked } = require('marked');
const fs = require('fs');
const path = require('path');

// Try local dir first (CI), then repo root (local dev)
let changelogPath = path.resolve(__dirname, 'CHANGELOG.md');
if (!fs.existsSync(changelogPath)) {
  changelogPath = path.resolve(__dirname, '..', '..', 'CHANGELOG.md');
  console.log('ℹ Using repo root CHANGELOG.md');
}
const outputPath = path.resolve(__dirname, 'changelog-content.html');

const md = fs.readFileSync(changelogPath, 'utf-8');

// Use default marked rendering
const html = marked.parse(md);

// Wrap in a styled container
const fullHtml = `<div class="changelog-content">${html}</div>`;

fs.writeFileSync(outputPath, fullHtml);
console.log(`✅ Changelog generated → ${outputPath}`);
