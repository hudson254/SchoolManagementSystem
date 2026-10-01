/**
 * Accommodation Reports â€” production browser verification.
 *
 * Runs against https://sms-server.school.internal (host-mapped) for
 * SYSTEM ADMINISTRATOR, ADMINISTRATOR, COORDINATOR, RECEPTIONIST and, as a
 * negative control, STUDENT.
 *
 * Sessions come from role_tokens.json: an access token minted for a REAL, EXISTING,
 * ACTIVE production user with exactly that user's real roles. Because /auth/me
 * resolves the profile from the database using the token's `sub`, the browser sees
 * the same identity, roles and authorization decisions it would after a real login.
 * No user is created, no password is changed, no data is written.
 *
 * Read-only: only GETs and the accommodation screens.
 */
const { chromium } = require('@playwright/test');
const fs = require('fs');
const path = require('path');

const BASE = process.env.BASE_URL || 'https://sms-server.school.internal';
const HOST_MAP = 'sms-server.school.internal 192.168.110.161';
const TOKENS = JSON.parse(fs.readFileSync(path.join('..', '.artifacts', 'accrep', 'role_tokens.json'), 'utf8'));
const SHOTS = process.env.SHOT_DIR || path.join('..', '.artifacts', 'accrep', 'shots');
fs.mkdirSync(SHOTS, { recursive: true });

const STAFF = ['SystemAdministrator', 'Administrator', 'Coordinator', 'Receptionist'];

// The report toggle buttons, exactly as rendered from REPORT_DEFINITIONS.
const REPORTS = [
  { btn: 'Current', key: 'current-occupancy' },
  { btn: 'Occupied', key: 'occupied-houses' },
  { btn: 'Empty', key: 'empty-houses' },
  { btn: 'History', key: 'occupancy-history' },
  { btn: 'House history', key: 'house-history' },
  { btn: 'By period', key: 'occupancy-by-period' },
  { btn: 'Occupant', key: 'occupant-history' },
  { btn: 'Utilization', key: 'utilization-summary' },
];

const results = [];
function record(role, area, name, ok, detail) {
  results.push({ role, area, name, ok, detail });
  console.log(
    `[${ok ? 'PASS' : 'FAIL'}] ${role.padEnd(20)} ${area.padEnd(10)} ${name}` +
      (detail ? ` :: ${detail}` : ''),
  );
}

/** Reads a summary tile ("At full capacity") as its rendered text. */
async function tile(page, label) {
  const loc = page.getByText(label, { exact: true }).first();
  await loc.waitFor({ state: 'visible', timeout: 15000 });
  const paper = loc.locator('xpath=ancestor::*[contains(@class,"MuiPaper-root")][1]');
  return ((await paper.innerText()) || '').replace(/\s+/g, ' ').trim();
}

async function newSession(browser, role) {
  const ctx = await browser.newContext({
    ignoreHTTPSErrors: true,
    acceptDownloads: true,
    viewport: { width: 1440, height: 1000 },
  });
  await ctx.addCookies([
    {
      name: 'access_token',
      value: TOKENS[role].access_token,
      domain: 'sms-server.school.internal',
      path: '/',
      secure: true,
      httpOnly: true,
      sameSite: 'Lax',
    },
  ]);
  const page = await ctx.newPage();
  const errors = [];
  const http5xx = [];
  page.on('pageerror', (e) => errors.push(e.message));
  page.on('console', (m) => {
    if (m.type() === 'error') errors.push('console: ' + m.text());
  });
  page.on('response', (r) => {
    if (r.status() >= 500) http5xx.push(`${r.status()} ${r.url()}`);
  });
  return { ctx, page, errors, http5xx };
}

const bodyText = async (page) => {
  await page.waitForTimeout(2500);
  return page.locator('body').innerText();
};
const hasErrorBoundary = (t) => /Something went wrong/i.test(t);

/**
 * Production throttles /api at 100 requests / 60 s (RATE_LIMIT_PERMIT /
 * RATE_LIMIT_WINDOW), and nginx at 10 r/s with burst 20. A 429 is an expected
 * response to aggressive automated traffic, NOT an application defect, so this
 * harness paces itself and treats a 429 as "back off and try again" rather than
 * as a functional failure.
 */
const sleep = (ms) => new Promise((r) => setTimeout(r, ms));

/** Navigates and waits for the SPA to settle; retries once after a rate-limit wait. */
async function gotoSettled(page, url, label = '') {
  for (let attempt = 1; attempt <= 3; attempt++) {
    await page.goto(url, { waitUntil: 'domcontentloaded' });
    await page.waitForTimeout(3500);
    if (!page.url().endsWith('/login')) return true;
    if (attempt < 3) {
      console.log(`  [pace] ${label} bounced to /login; waiting out the rate limiter (attempt ${attempt})`);
      await sleep(25000);
    }
  }
  return false;
}

module.exports = { BASE, HOST_MAP, TOKENS, SHOTS, STAFF, REPORTS, results, record, tile, newSession, bodyText, hasErrorBoundary, chromium, fs, path, sleep, gotoSettled };

/** Student must not reach the protected page, by UI or by direct API. */
async function verifyStudent(browser) {
  const role = 'Student';
  const { ctx, page, errors } = await newSession(browser, role);

  await gotoSettled(page, `${BASE}/dashboard`, 'Student dashboard');
  await page.waitForTimeout(2000);
  const shell = await bodyText(page);
  record(role, 'shell', 'app loads (dashboard)', !hasErrorBoundary(shell) && !/\/login$/.test(page.url()), page.url());

  const sideAccom = await page.getByRole('button', { name: 'Accommodation', exact: true }).count();
  record(role, 'sidebar', 'Accommodation entry hidden', sideAccom === 0, `Accommodation=${sideAccom}`);

  await gotoSettled(page, `${BASE}/accommodation/reports`, 'Student deep link');
  const t = await bodyText(page);
  const blocked =
    hasErrorBoundary(t) || /Accommodation reports are available/i.test(t) || /page not found/i.test(t);
  record(role, 'access', 'direct nav is blocked', blocked, blocked ? 'denied state' : t.slice(0, 100));

  const api = await page.evaluate(async () => {
    const r = await fetch('/api/v1/accommodation/reports/current-occupancy', { credentials: 'include' });
    return r.status;
  });
  record(role, 'access', 'direct API call is 403', api === 403, `status=${api}`);

  const apiExport = await page.evaluate(async () => {
    const r = await fetch('/api/v1/accommodation/reports/export?reportKey=current-occupancy&format=PDF', { credentials: 'include' });
    return r.status;
  });
  record(role, 'access', 'direct export API is 403', apiExport === 403, `status=${apiExport}`);

  record(role, 'runtime', 'no page errors', errors.length === 0, errors.slice(0, 2).join(' | ') || 'none');
  await ctx.close();
}

/** Full Accommodation Reports walkthrough for one authorized staff role. */
async function verifyStaff(browser, role) {
  const { ctx, page, errors, http5xx } = await newSession(browser, role);

  // ---------- shell + sidebar ----------
  await gotoSettled(page, `${BASE}/dashboard`, `${role} dashboard`);
  await page.waitForTimeout(2000);
  const shell = await bodyText(page);
  record(role, 'shell', 'app loads (dashboard)', !hasErrorBoundary(shell) && !/\/login$/.test(page.url()), page.url());

  // The Accommodation sidebar entry is a collapsible group; its children only
  // exist in the DOM once it is expanded, so expand it before looking for Reports.
  const accomGroup = page.getByRole('button', { name: 'Accommodation', exact: true }).last();
  const sideAccom = await page.getByRole('button', { name: 'Accommodation', exact: true }).count();
  if (sideAccom > 0) await accomGroup.click().catch(() => undefined);
  await page.waitForTimeout(1200);
  const sideReports = await page.getByRole('button', { name: 'Reports', exact: true }).count();
  record(role, 'sidebar', 'Accommodation + Reports entries present',
    sideAccom > 0 && sideReports > 0, `Accommodation=${sideAccom} Reports=${sideReports}`);
  if (sideReports > 0) {
    const href = await page.getByRole('button', { name: 'Reports', exact: true }).first().getAttribute('href').catch(() => null);
    record(role, 'sidebar', 'Reports entry points at /accommodation/reports',
      href === null || /accommodation\/reports/.test(href || ''), `href=${href}`);
  }

  // ---------- navigate Accommodation -> Reports ----------
  await gotoSettled(page, `${BASE}/accommodation`, `${role} accommodation`);
  const accom = await bodyText(page);
  record(role, 'accommodation', 'Accommodation page renders',
    !hasErrorBoundary(accom) && /Accommodation/.test(accom) && !/page not found/i.test(accom),
    `url=${new URL(page.url()).pathname}`);

  const reportsBtn = page.getByRole('button', { name: 'Reports', exact: true });
  if (await reportsBtn.count()) {
    await reportsBtn.first().click().catch(() => undefined);
  } else {
    await page.getByRole('button', { name: 'Accommodation', exact: true }).last().click().catch(() => undefined);
    await page.waitForTimeout(900);
    await page.getByRole('button', { name: 'Reports', exact: true }).first().click().catch(() => undefined);
  }
  await page.waitForURL('**/accommodation/reports', { timeout: 20000 }).catch(() => undefined);
  await page.waitForTimeout(3000);

  const text = await bodyText(page);
  const onRoute = new URL(page.url()).pathname === '/accommodation/reports';
  record(role, 'page', 'Accommodation Reports page renders',
    onRoute && !hasErrorBoundary(text) && /Accommodation Reports/.test(text),
    `url=${new URL(page.url()).pathname}`);
  await page.screenshot({ path: path.join(SHOTS, `${role}-reports.png`) });

  // ---------- every report ----------
  for (const r of REPORTS) {
    const before = errors.length;
    await page.getByRole('button', { name: r.btn, exact: true }).first().click();
    await page.waitForTimeout(3200);
    let t = await bodyText(page);

    if (r.key === 'occupancy-history') {
      // This endpoint requires a From and To date. The page must ask for one
      // instead of firing a request the API will reject with 400.
      const prompted = /Choose both a From and a To date/i.test(t);
      await page.fill('input[type="date"] >> nth=0', '2026-01-01');
      await page.waitForTimeout(1500);
      await page.fill('input[type="date"] >> nth=1', '2026-12-31');
      await page.waitForTimeout(3500);
      t = await bodyText(page);
      const loaded = /Distinct occupants/i.test(t) || /No occupancy in this period/i.test(t);
      record(role, 'report', r.key, prompted && loaded && !hasErrorBoundary(t),
        prompted ? 'prompted for a period, then loaded' : t.slice(0, 90));
      continue;
    }

    const newErrs = errors.slice(before);
    const ok = !hasErrorBoundary(t) && newErrs.length === 0;
    record(role, 'report', r.key, ok,
      ok ? (/Choose a house/i.test(t) ? 'rendered (awaiting required house)' : 'rendered') : (newErrs[0] || t.slice(0, 90)));
  }

  // ---------- DEFECT-02: capacity tiles on the period report ----------
  await page.getByRole('button', { name: 'By period', exact: true }).first().click();
  await page.waitForTimeout(3500);
  const total = await tile(page, 'Total houses');
  const full = await tile(page, 'At full capacity');
  const free = await tile(page, 'With free space');
  const never = await tile(page, 'Never occupied');
  const tileOk =
    /At full capacity\s*\d+/.test(full) &&
    /With free space\s*\d+/.test(free) &&
    /Never occupied\s*\d+/.test(never);
  record(role, 'defect-02', 'period capacity tiles populated', tileOk,
    `${total} | ${full} | ${free} | ${never}`);
  await page.screenshot({ path: path.join(SHOTS, `${role}-byperiod.png`) });

  // ---------- filters ----------
  const lane = page.getByRole('combobox', { name: /lane/i }).first();
  if (await lane.count()) {
    await lane.click();
    await page.waitForTimeout(1000);
    const opt = page.getByRole('option').first();
    if (await opt.count()) {
      await opt.click();
      await page.waitForTimeout(2800);
      record(role, 'filter', 'lane filter applies', !hasErrorBoundary(await bodyText(page)), '');
    } else {
      await page.keyboard.press('Escape');
    }
  }
  const search = page.getByLabel(/search/i).first();
  if (await search.count()) {
    await search.fill('H-0');
    await page.getByRole('button', { name: /^apply$/i }).first().click();
    await page.waitForTimeout(2800);
    record(role, 'filter', 'search filter applies', !hasErrorBoundary(await bodyText(page)), '');
  }

  // ---------- exports ----------
  for (const fmt of ['PDF', 'Excel']) {
    const dl = page.waitForEvent('download', { timeout: 45000 }).catch(() => null);
    await page.getByRole('button', { name: fmt, exact: true }).first().click();
    const d = await dl;
    if (!d) {
      record(role, 'export', `${fmt} download`, false, 'no download event');
      continue;
    }
    const ext = fmt === 'PDF' ? 'pdf' : 'xlsx';
    const dest = path.join(SHOTS, `${role}-${fmt}.${ext}`);
    await d.saveAs(dest);
    const buf = fs.readFileSync(dest);
    const magic = fmt === 'PDF' ? buf.subarray(0, 5).toString() : buf.subarray(0, 2).toString();
    const valid = fmt === 'PDF' ? magic === '%PDF-' : magic === 'PK';
    record(role, 'export', `${fmt} file is valid`, valid && buf.length > 1000,
      `name=${d.suggestedFilename()} bytes=${buf.length} magic=${JSON.stringify(magic)}`);
  }

  // ---------- direct navigation + refresh ----------
  await gotoSettled(page, `${BASE}/accommodation/reports`, `${role} deep link`);
  const t2 = await bodyText(page);
  record(role, 'nav', 'direct URL load', !hasErrorBoundary(t2) && /Accommodation Reports/.test(t2), page.url());

  await page.reload({ waitUntil: 'domcontentloaded' });
  await page.waitForTimeout(5000);
  const t3 = await bodyText(page);
  record(role, 'nav', 'browser refresh keeps the page', !hasErrorBoundary(t3) && /Accommodation Reports/.test(t3), page.url());

  // 5xx is an application defect; a 429 is the expected rate-limiter answer to
  // automated traffic and is counted separately.
  const real5xx = http5xx.filter((h) => !h.startsWith('429'));
  const rateLimited = http5xx.filter((h) => h.startsWith('429'));
  record(role, 'runtime', 'no console/page errors', errors.length === 0, errors.slice(0, 2).join(' | ') || 'none');
  record(role, 'runtime', 'no HTTP 5xx', real5xx.length === 0,
    real5xx.length ? real5xx.slice(0, 2).join(' | ') : `429(rate-limited)=${rateLimited.length}`);

  await ctx.close();
}

(async () => {
  // Let the limiter window drain before the first navigation.
  console.log('[pace] waiting 70s for the rate-limit window to drain...');
  await sleep(70000);

  const browser = await chromium.launch({ args: [`--host-resolver-rules=MAP ${HOST_MAP}`] });
  for (const role of STAFF) {
    try {
      await verifyStaff(browser, role);
    } catch (e) {
      record(role, 'harness', 'role walkthrough completed', false, String(e).slice(0, 200));
    }
    // Pace between roles: production allows 100 API requests per 60 s.
    await sleep(15000);
  }
  try {
    await verifyStudent(browser);
  } catch (e) {
    record('Student', 'harness', 'negative walkthrough completed', false, String(e).slice(0, 200));
  }
  await browser.close();

  const failed = results.filter((r) => !r.ok);
  console.log('\n================ SUMMARY ================');
  console.log(`Total checks : ${results.length}`);
  console.log(`Passed       : ${results.length - failed.length}`);
  console.log(`Failed       : ${failed.length}`);
  failed.forEach((f) => console.log(`  FAIL ${f.role} / ${f.area} / ${f.name} :: ${f.detail}`));
  fs.writeFileSync(path.join('..', '.artifacts', 'accrep', 'browser-results.json'), JSON.stringify(results, null, 2));
  process.exit(failed.length ? 1 : 0);
})().catch((e) => {
  console.error('HARNESS ERROR', e);
  process.exit(2);
});


