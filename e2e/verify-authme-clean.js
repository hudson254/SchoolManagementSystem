/** Minimal, low-volume confirmation after the 15-minute rate-limit ban clears. */
const { chromium } = require('@playwright/test');

const BASE = 'https://sms-server.school.internal';
const sleep = (ms) => new Promise((r) => setTimeout(r, ms));

(async () => {
  // The ban is 15 minutes; the last breach was at 20:08:52.
  console.log('[pace] waiting 16 minutes for the ban to clear...');
  await sleep(16 * 60 * 1000);

  const browser = await chromium.launch({
    args: ['--host-resolver-rules=MAP sms-server.school.internal 192.168.110.161'],
  });
  const ctx = await browser.newContext({ ignoreHTTPSErrors: true });
  const page = await ctx.newPage();

  // ONE login, ONE page load, ONE /auth/me. Nothing that could re-trip the limiter.
  await page.goto(`${BASE}/login`, { waitUntil: 'domcontentloaded' });
  await page.getByLabel(/email or username/i).first().waitFor({ state: 'visible', timeout: 30000 });
  await page.getByLabel(/email or username/i).first().fill(process.env.ADMIN_EMAIL);
  await page.getByLabel(/password/i).first().fill(process.env.ADMIN_PASSWORD);
  await page.getByRole('button', { name: /sign in/i }).click();
  await page.waitForURL(/\/(dashboard|course-selection)/, { timeout: 60000 });
  await page.waitForTimeout(8000);

  const me = await page.evaluate(async () => {
    const r = await fetch('/api/v1/auth/me', { credentials: 'include' });
    return { status: r.status, body: (await r.text()).slice(0, 160) };
  });
  console.log(`[/auth/me status=${me.status}] ${me.status === 200 ? 'PASS' : 'FAIL'} :: ${me.body}`);

  const url = new URL(page.url()).pathname;
  console.log(`[login -> ${url}] ${url !== '/login' ? 'PASS' : 'FAIL'}`);

  await browser.close();
})().catch((e) => { console.error('HARNESS ERROR', e); process.exit(2); });
