import { defineConfig, devices } from '@playwright/test';

/**
 * End-to-end tests against the full stack: the backend from docker-compose.e2e.yml (started by CI or by
 * hand) and the SSR app built with `ng build --configuration e2e`, which this config starts on :4000.
 */
const appDir = '../frontend/open-planning-poker-web-app';

export default defineConfig({
  testDir: './tests',
  timeout: 60_000,
  expect: { timeout: 15_000 },
  fullyParallel: true,
  forbidOnly: !!process.env['CI'],
  retries: process.env['CI'] ? 1 : 0,
  workers: process.env['CI'] ? 2 : undefined,
  reporter: process.env['CI'] ? [['github'], ['html', { open: 'never' }]] : 'list',
  use: {
    baseURL: 'http://localhost:4000',
    trace: 'retain-on-failure',
    screenshot: 'only-on-failure',
    video: 'retain-on-failure',
  },
  projects: [
    { name: 'chromium', use: { ...devices['Desktop Chrome'] } },
    { name: 'mobile', use: { ...devices['Pixel 7'] }, testMatch: /smoke\.spec\.ts/ },
  ],
  webServer: {
    command: 'node dist/open-planning-poker-ui/server/server.mjs',
    cwd: appDir,
    url: 'http://localhost:4000',
    env: { PORT: '4000' },
    reuseExistingServer: !process.env['CI'],
    timeout: 60_000,
  },
});
