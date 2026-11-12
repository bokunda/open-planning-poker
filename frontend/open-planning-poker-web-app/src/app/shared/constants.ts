import { version } from '../../../package.json';
export const OPP_GITHUB_REPO_URL = 'https://github.com/bokunda/open-planning-poker';
export const OPP_WEBSITE_URL = 'https://openplanningpoker.com';
// Single source of truth: the "version" in package.json (bump it for every release).
export const OPP_APP_VERSION = version;

export const OPP_SNACKBAR_DURATION_DEFAULT = 2000;
export const OPP_SNACKBAR_MODAL_LABEL_CLOSE = 'Close';

/**
 * Donation pages, in one place for the app (header menu, footer, About) and the README.
 * GitHub Sponsors takes no fee; Ko-fi takes one-off "coffees" without an account.
 */
export const OPP_SUPPORT = {
  githubSponsors: 'https://github.com/sponsors/bokunda',
  koFi: 'https://ko-fi.com/bokunda',
} as const;
