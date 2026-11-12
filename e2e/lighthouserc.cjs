// Lighthouse CI (OPP-32). CI serves the SSR app on :4000 (local backend) and the website build on :8080,
// and passes a freshly created game room as LHCI_GAME_URL.
const app = 'http://localhost:4000';
const site = 'http://localhost:8080';

// robots.txt carries an `Agentmap:` line for agent discovery (ARD). Lighthouse reports unknown directives
// and takes 8 points off SEO for it; search engines ignore them. So the SEO category allows that one audit
// (0.92), and every other SEO audit must pass on its own.
const seoAudits = ['document-title', 'meta-description', 'http-status-code', 'link-text', 'crawlable-anchors',
  'is-crawlable', 'hreflang', 'canonical', 'image-alt'];

const strict = {
  'categories:performance': ['error', { minScore: 0.9 }],
  'categories:accessibility': ['error', { minScore: 1 }],
  'categories:best-practices': ['error', { minScore: 0.95 }],
  'categories:seo': ['error', { minScore: 0.92 }],
  ...Object.fromEntries(seoAudits.map((id) => [id, ['error', { minScore: 1 }]])),
  'robots-txt': 'warn',
};

module.exports = {
  ci: {
    collect: {
      url: [
        `${app}/game`,
        process.env.LHCI_GAME_URL,
        `${site}/`,
        `${site}/what-is-planning-poker/`,
        `${site}/changelog/`,
      ].filter(Boolean),
      numberOfRuns: 3,
      settings: {
        preset: 'desktop',
        chromeFlags: '--no-sandbox',
        // Analytics (GA4, Clarity) are not part of the page budget; Clarity sets third-party cookies.
        blockedUrlPatterns: ['*googletagmanager.com*', '*google-analytics.com*', '*clarity.ms*', '*bing.com*'],
      },
    },
    assert: {
      assertMatrix: [
        { matchingUrlPattern: '/game/[0-9a-f-]{36}', assertions: {
          // Rooms are noindex by design and load live data, so SEO and performance are not budgeted here.
          'categories:accessibility': ['error', { minScore: 1 }],
          'categories:best-practices': ['error', { minScore: 0.95 }],
        } },
        { matchingUrlPattern: '^(?!.*/game/[0-9a-f-]{36}).*$', assertions: strict },
      ],
    },
    upload: { target: 'filesystem', outputDir: './lighthouse-report' },
  },
};
