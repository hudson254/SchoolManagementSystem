/**
 * Production regression sweep after the Accommodation Reports frontend repair.
 *
 * Signs in with the REAL System Administrator credentials (proving the password
 * login flow itself, not just an injected session), then walks the modules the
 * repair could have disturbed - with particular attention to the sidebar, which
 * gained a navigation entry.
 *
 * Paced to stay under production's 100 req/60 s API limiter.
 */
const { chromium } = require('@playwright/test');
const fs = require('fs');
const path = require('path');

const BASE = process.env.BASE_URL || 'https://sms-server.school.internal';
const SHOTS = path.join('..', '.artifacts', 'accrep', 'shots');
fs.mkdirSync(SHOTS, { recursive: true });

const results = [];
const rec = (area, name, ok, detail) => {
  results.push({ area, name, ok, detail });
  console.log(`[${ok ? 'PASS' : 'FAIL'}] ${area.padEnd(14)} ${name}${detail ? ` :: ${detail}` : ''}`);
};

const PAGES = [
  { name: 'Dashboard',         path: '/dashboard',         nav: ['Dashboard'] },
  { name: 'Accommodation',     path: '/accommodation',     nav: ['Accommodation', 'Houses & Allocation'] },
  { name: 'Accommodation Reports', path: '/accommodation/reports', nav: ['Accommodation', 'Reports'] },
  { name: 'Students',          path: '/students',          nav: ['Students'] },
  { name: 'Lecturers',         path: '/lecturers',         nav: ['Lecturers'] },
  { name: 'Classes',           path: '/classes',           nav: ['Academic', 'Classes'] },
  { name: 'Timetable',         path: '/timetable',         nav: ['Academic', 'Timetable'] },
  { name: 'Courses',           path: '/courses',           nav: ['Academic', 'Courses'] },
  { name: 'Units',             path: '/units',             nav: ['Academic', 'Units'] },
  { name: 'Course Offerings',  path: '/course-offerings',  nav: ['Academic', 'Course Offerings'] },
  { name: 'Assignments',       path: '/assignments',       nav: ['Assignments'] },
  { name: 'OMS Dashboard',     path: '/oms',               nav: ['Order Management', 'OMS Dashboard'] },
  { name: 'OMS Orders',        path: '/oms/orders',        nav: ['Order Management', 'Orders'] },
  { name: 'Request Workspace', path: '/oms/requests',      nav: ['Requests', 'Request Workspace'] },
  { name: 'Notifications',     path: '/notifications',     nav: ['Notifications'] },
  { name: 'Settings',          path: '/settings',          nav: ['Settings'] },
];

/** Clicks the labelled sidebar button positioned lowest on the page. */
async function clickSidebar(page, label) {
  const m = page.getByRole('button', { name: label, exact: true });
  if ((await m.count()) === 0) return false;
  let target = m.first();
  let bestY = -1;
  for (let i = 0; i < (await m.count()); i++) {
    const y = (await m.nth(i).boundingBox())?.y ?? -1;
    if (y > bestY) { bestY = y; target = m.nth(i); }
  }
  await target.click({ timeout: 10000 }).catch(() => undefined);
  return true;
}

(async () => {
  const email = process.env.ADMIN_EMAIL;
  const password = process.env.ADMIN_PASSWORD;
  if (!email || !password) {
    console.error('ADMIN_EMAIL / ADMIN_PASSWORD are required');
    process.exit(2);
  }

  const browser = await chromium.launch({
    args: ['--host-resolver-rules=MAP sms-server.school.internal 192.168.110.161'],
  });
  const ctx = await browser.newContext({ ignoreHTTPSErrors: true, viewport: { width: 1440, height: 1000 } });
  const page = await ctx.newPage();
  const pageErrors = [];
  page.on('pageerror', (e) => pageErrors.push(e.message));

  // ---------- 1. real password login ----------
  await page.goto(`${BASE}/login`, { waitUntil: 'domcontentloaded' });
  await page.getByLabel(/email or username/i).first().waitFor({ state: 'visible', timeout: 30000 });
  await page.getByLabel(/email or username/i).first().fill(email);
  await page.getByLabel(/password/i).first().fill(password);
  await page.getByRole('button', { name: /sign in/i }).click();
  await page.waitForURL(/\/(dashboard|course-selection)/, { timeout: 60000 });
  await page.waitForTimeout(4000);
  rec('login', 'real credential sign-in', !/\/login$/.test(page.url()), page.url());
  const me = await page.evaluate(async () => {
    const r = await fetch('/api/v1/auth/me', { credentials: 'include' });
    return r.status;
  });
  rec('login', '/auth/me after login', me === 200, `status=${me}`);

  // ---------- 2. sidebar structure ----------
  for (const g of ['Academic', 'Order Management', 'Requests']) await clickSidebar(page, g);
  await page.waitForTimeout(1500);
  const sidebarText = await page.locator('aside, nav').first().innerText().catch(() => '');
  rec('sidebar', 'Accommodation group present', /Accommodation/.test(sidebarText), '');
  rec('sidebar', 'Accommodation > Reports entry present', /Reports/.test(sidebarText), '');
  rec('sidebar', 'other groups still render',
    ['Academic', 'Order Management', 'Requests'].every((g) => sidebarText.includes(g)), '');

  // ---------- 3. walk every module ----------
  for (const p of PAGES) {
    pageErrors.length = 0;

    if (p.nav.length === 2) {
      // Child of a collapsible group: make sure the group is open first.
      const child = page.getByRole('button', { name: p.nav[1], exact: true }).first();
      if (!(await child.isVisible().catch(() => false))) {
        await clickSidebar(page, p.nav[0]);
        await page.waitForTimeout(1200);
      }
      await clickSidebar(page, p.nav[1]);
    } else {
      await clickSidebar(page, p.nav[0]);
    }

    await page.waitForURL(`**${p.path}`, { timeout: 20000 }).catch(() => undefined);
    await page.waitForTimeout(3500);

    const body = await page.locator('body').innerText();
    const url = new URL(page.url()).pathname;
    const boundary = /Something went wrong/i.test(body);
    const notFound = /page not found/i.test(body);
    const bounced = /\/login$/.test(url);
    const ok = !boundary && !bounced && body.length > 200 && pageErrors.length === 0;
    rec('regression', p.name, ok,
      `url=${url} len=${body.length} boundary=${boundary} notFound=${notFound} jsErrors=${pageErrors.length}` +
        (pageErrors.length ? ` :: ${pageErrors[0].slice(0, 120)}` : ''));
    await page.waitForTimeout(1500);
  }

  await browser.close();

  const failed = results.filter((r) => !r.ok);
  console.log('\n================ SUMMARY ================');
  console.log(`Total : ${results.length}  Passed : ${results.length - failed.length}  Failed : ${failed.length}`);
  failed.forEach((f) => console.log(`  FAIL ${f.area} / ${f.name} :: ${f.detail}`));
  process.exit(failed.length ? 1 : 0);
})().catch((e) => { console.error('HARNESS ERROR', e); process.exit(2); });
