// Prints every Lighthouse audit that did not pass, per URL, from the reports lhci wrote to ./lighthouse-report,
// so a failed budget can be read in the CI log without downloading the artifact.
const fs = require('fs');
const path = require('path');
const dir = path.resolve(__dirname, '..', 'lighthouse-report');
if (!fs.existsSync(dir)) process.exit(0);
const seen = new Set();
for (const file of fs.readdirSync(dir).filter((f) => f.endsWith('.json') && f.startsWith('localhost'))) {
  const lhr = JSON.parse(fs.readFileSync(path.join(dir, file), 'utf-8'));
  for (const [id, category] of Object.entries(lhr.categories)) {
    for (const ref of category.auditRefs.filter((r) => r.weight > 0)) {
      const audit = lhr.audits[ref.id];
      if (audit.score === null || audit.score >= 1) continue;
      const key = `${lhr.finalDisplayedUrl} ${ref.id}`;
      if (seen.has(key)) continue;
      seen.add(key);
      const nodes = (audit.details?.items ?? []).slice(0, 3)
        .map((item) => item.node
          ? `${item.node.selector} ${item.node.snippet}\n      ${(item.node.explanation ?? '').replace(/\s+/g, ' ')}`
          : item.url ?? '')
        .filter(Boolean);
      console.log(`${lhr.finalDisplayedUrl} [${id}] ${ref.id}: ${audit.title}${nodes.length ? `\n    ${nodes.join('\n    ')}` : ''}`);
    }
  }
}
