/**
 * Generates changelog.html partial from root CHANGELOG.md using marked.
 * Outputs Tailwind-styled HTML ready for inclusion in the website.
 */
const { marked } = require('marked');
const fs = require('fs');
const path = require('path');

const changelogPath = path.resolve(__dirname, '..', '..', 'CHANGELOG.md');
const outputPath = path.resolve(__dirname, 'changelog-content.html');

const md = fs.readFileSync(changelogPath, 'utf-8');

// Custom renderer for Tailwind styling
const renderer = new marked.Renderer();

renderer.heading = (text, level) => {
  if (level === 1) return ''; // Skip main title
  if (level === 2) {
    // Version headings
    return `<h2 class="text-2xl font-bold text-gray-900 dark:text-white mt-12 mb-4 pb-2 border-b border-gray-200 dark:border-gray-700">${text}</h2>`;
  }
  if (level === 3) {
    return `<h3 class="text-lg font-semibold text-purple-600 dark:text-purple-400 mt-6 mb-2">${text}</h3>`;
  }
  return `<h${level}>${text}</h${level}>`;
};

renderer.list = (body, ordered) => {
  const tag = ordered ? 'ol' : 'ul';
  return `<${tag} class="list-disc pl-5 space-y-1 text-gray-600 dark:text-gray-400 mt-2 mb-4">${body}</${tag}>`;
};

renderer.listitem = (text) => {
  return `<li class="pl-1">${text}</li>`;
};

renderer.paragraph = (text) => {
  return `<p class="text-gray-600 dark:text-gray-400 mt-2 mb-4">${text}</p>`;
};

renderer.strong = (text) => {
  return `<strong class="font-semibold text-gray-900 dark:text-white">${text}</strong>`;
};

renderer.codespan = (code) => {
  return `<code class="bg-gray-100 dark:bg-gray-700 text-purple-600 dark:text-purple-400 px-1.5 py-0.5 rounded text-sm">${code}</code>`;
};

marked.setOptions({ renderer });

const html = marked.parse(md);

// Wrap in a section
const fullHtml = `<section class="changelog-content prose max-w-none">${html}</section>`;

fs.writeFileSync(outputPath, fullHtml);
console.log(`✅ Changelog generated → ${outputPath}`);
