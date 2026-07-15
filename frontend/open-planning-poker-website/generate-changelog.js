/**
 * Generates changelog-content.html partial from root CHANGELOG.md using marked.
 * Uses default renderer + scoped Tailwind CSS for styling.
 */
const { marked } = require('marked');
const fs = require('fs');
const path = require('path');

const changelogPath = path.resolve(__dirname, '..', '..', 'CHANGELOG.md');
const outputPath = path.resolve(__dirname, 'changelog-content.html');

const md = fs.readFileSync(changelogPath, 'utf-8');

// Use default marked rendering
const html = marked.parse(md);

// Wrap in a styled container
const fullHtml = `<div class="changelog-content">${html}</div>`;

fs.writeFileSync(outputPath, fullHtml);
console.log(`✅ Changelog generated → ${outputPath}`);
