// Checks that every language has the same keys as English and that the templates only use keys that exist.
// Run: node scripts/check-i18n.mjs (CI runs it in the frontend job).
import { readFileSync, readdirSync, statSync } from 'node:fs';
import { join } from 'node:path';

const dir = new URL('../src/app/i18n/', import.meta.url);
const load = (lang) => {
  const source = readFileSync(new URL(`${lang}.ts`, dir), 'utf-8')
    .replace(/^\/\/.*$/gm, '')
    .replace('const translation =', 'return')
    .replace('export default translation;', '');
  return new Function(source)();
};
const flatten = (obj, prefix = '') => Object.entries(obj).flatMap(([k, v]) =>
  typeof v === 'object' ? flatten(v, `${prefix}${k}.`) : [[`${prefix}${k}`, v]]);

const en = new Map(flatten(load('en')));
let failed = false;
for (const lang of ['sr', 'de']) {
  const other = new Map(flatten(load(lang)));
  const missing = [...en.keys()].filter((k) => !other.has(k));
  const extra = [...other.keys()].filter((k) => !en.has(k));
  const params = [...en.keys()].filter((k) => other.has(k) &&
    JSON.stringify((en.get(k).match(/{{\w+}}/g) ?? []).sort()) !== JSON.stringify((other.get(k).match(/{{\w+}}/g) ?? []).sort()));
  for (const [label, list] of [['missing', missing], ['extra', extra], ['different {{params}}', params]]) {
    if (list.length) { failed = true; console.error(`${lang}: ${label} keys: ${list.join(', ')}`); }
  }
}

const files = [];
const walk = (path) => readdirSync(path).forEach((name) => {
  const full = join(path, name);
  if (statSync(full).isDirectory()) { if (name !== 'i18n') walk(full); }
  else if (/\.(ts|html)$/.test(name) && !name.endsWith('.spec.ts')) files.push(full);
});
walk(new URL('../src/app/', import.meta.url).pathname.replace(/^\/([A-Za-z]:)/, '$1'));
const used = new Set();
for (const file of files) {
  const text = readFileSync(file, 'utf-8');
  for (const m of text.matchAll(/'([a-zA-Z]+\.[a-zA-Z0-9.]+)'\s*\|\s*transloco/g)) used.add(m[1]);
  for (const m of text.matchAll(/translate\('([a-zA-Z]+\.[a-zA-Z0-9.]+)'/g)) used.add(m[1]);
}
const unknown = [...used].filter((k) => !en.has(k));
if (unknown.length) { failed = true; console.error(`unknown keys used: ${unknown.join(', ')}`); }

console.log(`i18n: ${en.size} keys, ${used.size} used directly, languages en/sr/de`);
process.exit(failed ? 1 : 0);
