/** Re-confirms the two harness artifacts from the regression sweep. */
const { chromium } = require('@playwright/test');

const BASE = 'https://sms-server.school.internal';
const sleep = (ms) => new Promise((r) => setTimeout(r, ms));

(async () => {
  const browser = await chromium.launch({
    args: ['--host-resolver-rules=MAP sms-server.school.internal 192.168.110.161'],
  });
  const ctx = await browser.newContext({ ignoreHTTPSErrors: true, viewport: { width: 1440, height: 1000 } });
  const page = await ctx.newPage();

  console.log('[pace] waiting 70s for the rate-limit window...');
  await sleep(70000);

  await page.goto(`${BASE}/login`, { waitUntil: 'domcontentloaded' });
  await page.getByLabel(/email or username/i).first().waitFor({ state: 'visible', timeout: 30000 });
  await page.getByLabel(/email or username/i).first().fill(process.env.ADMIN_EMAIL);
  await page.getByLabel(/password/i).first().fill(process.env.ADMIN_PASSWORD);
  await page.getByRole('button', { name: /sign in/i }).click();
  await page.waitForURL(/\/(dashboard|course-selection)/, { timeout: 60000 });
  await page.waitForTimeout(6000);

  // 1) /auth/me, retried so a 429 is not mistaken for an authentication failure.
  for (let i = 1; i <= 4; i++) {
    const me = await page.evaluate(async () => {
      const r = await fetch('/api/v1/auth/me', { credentials: 'include' });
      return r.status;
    });
    console.log(`[${me === 200 ? 'PASS' : 'FAIL'}] login   /auth/me after real login :: status=${me} (attempt ${i})`);
    if (me === 200) break;
    await sleep(25000);
  }

  // 2) Sidebar: expand the Accommodation group, then read it.
  const group = page.getByRole('button', { name: 'Accommodation', exact: true }).last();
  const existed = (await page.getByRole('button', { name: 'Accommodation', exact: true }).count()) > 0;
  if (existed) await group.click().catch(() => undefined);
  await page.waitForTimeout(1500);
  const text = await page.locator('aside, nav').first().innerText().catch(() => '');
  const hasReports = /Reports/.test(text);
  console.log(`[${existed && hasReports ? 'PASS' : 'FAIL'}] sidebar Accommodation group expands to a Reports entry :: group=${existed} reports=${hasReports}`);
  console.log('sidebar excerpt:', text.split('\n').filter((l) => /Accommodation|Reports|Houses/.test(l)).join(' | '));

  // 3) Clicking the sidebar entry must land on the reports route.
  const reports = page.getByRole('button', { name: 'Reports', exact: true }).first();
  if (await reports.count()) {
    await reports.click();
    await page.waitForTimeout(4000);
    const url = new URL(page.url()).pathname;
    const body = await page.locator('body').innerText();
    console.log(`[${url === '/accommodation/reports' ? 'PASS' : 'FAIL'}] nav      sidebar Reports navigates to the report route :: url=${url}`);
    console.log(`[${/Accommodation Reports/.test(body) && !/Something went wrong/i.test(body) ? 'PASS' : 'FAIL'}] nav      report page renders after sidebar click :: len=${body.length}`);
  }

  await browser.close();
})().catch((e) => { console.error('HARNESS ERROR', e); process.exit(2); });
