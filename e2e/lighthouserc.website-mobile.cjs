// Lighthouse CI for every page of the website on a phone (OPP-181). The desktop run (lighthouserc.cjs) covers the app;
// this one walks the website build on :8080 with Lighthouse's default mobile emulation and slow 4G throttling.
const fs = require('fs');
const path = require('path');

const site = 'http://localhost:8080';
const dist = path.resolve(__dirname, '../frontend/open-planning-poker-website/dist');

// Every English page plus the Serbian and German home pages; the translations share the same markup.
const pages = fs.existsSync(dist)
  ? fs.readdirSync(dist, { withFileTypes: true })
    .filter((d) => d.isDirectory() && fs.existsSync(path.join(dist, d.name, 'index.html')) && !['sr', 'de'].includes(d.name))
    .map((d) => `${site}/${d.name}/`)
  : [];

const seoAudits = ['document-title', 'meta-description', 'http-status-code', 'link-text', 'crawlable-anchors',
  'is-crawlable', 'hreflang', 'canonical', 'image-alt'];

module.exports = {
  ci: {
    collect: {
      url: [`${site}/`, `${site}/sr/`, `${site}/de/`, ...pages],
      numberOfRuns: 1,
      settings: {
        chromeFlags: '--no-sandbox',
        blockedUrlPatterns: ['*googletagmanager.com*', '*google-analytics.com*', '*clarity.ms*', '*bing.com*', '*openplanningpoker.com/site/visit*'],
      },
    },
    assert: {
      assertions: {
        // Core Web Vitals in the green on a phone.
        'largest-contentful-paint': ['error', { maxNumericValue: 2500 }],
        'cumulative-layout-shift': ['error', { maxNumericValue: 0.1 }],
        'total-blocking-time': ['error', { maxNumericValue: 200 }],
        'categories:performance': ['error', { minScore: 0.9 }],
        'categories:accessibility': ['error', { minScore: 1 }],
        'categories:seo': ['error', { minScore: 0.92 }],
        ...Object.fromEntries(seoAudits.map((id) => [id, ['error', { minScore: 1 }]])),
        'robots-txt': 'warn',
      },
    },
    upload: { target: 'filesystem', outputDir: './lighthouse-report-website' },
  },
};
