import { defineConfig, devices } from '@playwright/test';

/**
 * `PW_HOST_MAP` lets tests reach an internal hostname that the local machine
 * cannot resolve (e.g. `sms-server.school.internal=192.168.110.161`). The Host
 * header stays the real hostname so TLS and CORS behave exactly as in a browser.
 */
const hostMap = (process.env.PW_HOST_MAP || '')
  .split(',')
  .map((pair) => pair.split('='))
  .filter((parts): parts is [string, string] => parts.length === 2 && !!parts[0] && !!parts[1])
  .map(([host, ip]) => ({ host: host.trim(), ip: ip.trim() }));

export default defineConfig({
  testDir: './tests',
  fullyParallel: false,
  retries: 1,
  workers: 1,
  reporter: [
    ['html', { outputFolder: 'playwright-report' }],
    ['list']
  ],
  use: {
    baseURL: process.env.BASE_URL || 'http://localhost:5000',
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
    video: 'retain-on-failure',
  },
  projects: [
    {
      name: 'chromium',
      use: {
        ...devices['Desktop Chrome'],
        ...(hostMap.length
          ? {
              launchOptions: {
                args: hostMap.flatMap(({ host, ip }) => [
                  `--host-resolver-rules=MAP ${host} ${ip}`,
                ]),
              },
            }
          : {}),
      },
    },
  ],
});