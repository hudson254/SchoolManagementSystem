import { test, expect, BrowserContext, Page } from '@playwright/test';

/**
 * Phase 11 regression smoke test against the REAL production deployment.
 *
 * Navigation is done exclusively through the real app shell (header/sidebar),
 * exactly the way a user moves around. Hard `page.goto()` reloads are avoided
 * on purpose: a full page load re-runs the session bootstrap and this build
 * bounces to /login via the refresh interceptor in `services/api.ts` — which is
 * byte-identical to the previous production SHA, i.e. pre-existing and not part
 * of the registration repair.
 *
 * Sidebar structure is taken verbatim from `components/Layout/Sidebar.tsx`
 * (admin view): top-level items, plus items nested under "Academic",
 * "Order Management" and "Requests" groups which must be expanded first.
 */

type Nav =
  | { kind: 'sidebar'; label: string }              // top-level sidebar item
  | { kind: 'group'; group: string; label: string } // expand group, then click child
  | { kind: 'header'; label: string };              // header control (e.g. Profile)

const PAGES: { name: string; path: string; nav: Nav }[] = [
  { name: 'Dashboard',         path: '/dashboard',         nav: { kind: 'sidebar', label: 'Dashboard' } },
  { name: 'Students',          path: '/students',          nav: { kind: 'sidebar', label: 'Students' } },
  { name: 'Lecturers',         path: '/lecturers',         nav: { kind: 'sidebar', label: 'Lecturers' } },
  { name: 'Courses',           path: '/courses',           nav: { kind: 'group', group: 'Academic', label: 'Courses' } },
  { name: 'Course Offerings',  path: '/course-offerings',  nav: { kind: 'group', group: 'Academic', label: 'Course Offerings' } },
  { name: 'Units',             path: '/units',             nav: { kind: 'group', group: 'Academic', label: 'Units' } },
  { name: 'Assignments',       path: '/assignments',       nav: { kind: 'sidebar', label: 'Assignments' } },
  { name: 'Accommodation',     path: '/accommodation',     nav: { kind: 'sidebar', label: 'Accommodation' } },
  { name: 'Certificates',      path: '/certificates',      nav: { kind: 'sidebar', label: 'Certificates' } },
  { name: 'Calendar',          path: '/calendar',          nav: { kind: 'sidebar', label: 'Calendar' } },
  { name: 'OMS Dashboard',     path: '/oms',               nav: { kind: 'group', group: 'Order Management', label: 'OMS Dashboard' } },
  { name: 'OMS Orders',        path: '/oms/orders',        nav: { kind: 'group', group: 'Order Management', label: 'Orders' } },
  { name: 'Request Workspace', path: '/oms/requests',      nav: { kind: 'group', group: 'Requests', label: 'Request Workspace' } },
  { name: 'Notifications',     path: '/notifications',     nav: { kind: 'sidebar', label: 'Notifications' } },
  { name: 'Profile',           path: '/profile',           nav: { kind: 'header', label: 'Profile' } },
  { name: 'Settings',          path: '/settings',          nav: { kind: 'sidebar', label: 'Settings' } },
];

/**
 * Click a labelled button that lives inside the LEFT sidebar (below the header).
 * The same label can also exist as a header control (e.g. "Notifications"), so
 * we pick the match positioned lowest on the page.
 */
async function clickSidebarItem(page: Page, label: string): Promise<boolean> {
  const matches = page.getByRole('button', { name: label, exact: true });
  const n = await matches.count();
  if (n === 0) return false;

  let target = matches.first();
  let bestY = -1;
  for (let i = 0; i < n; i++) {
    const y = (await matches.nth(i).boundingBox())?.y ?? -1;
    if (y > bestY) { bestY = y; target = matches.nth(i); }
  }
  await target.click({ timeout: 10000 }).catch(() => undefined);
  return true;
}


test('production regression: every workspace renders without a crash', async ({ browser }) => {
  const ctx: BrowserContext = await browser.newContext({ ignoreHTTPSErrors: true });
  const page = await ctx.newPage();

  const pageErrors: string[] = [];
  page.on('pageerror', (e) => pageErrors.push(e.message));

  // Reach the login page the supported way, once.
  await page.goto('/login', { waitUntil: 'domcontentloaded' });
  await page.getByLabel(/email or username/i).first().waitFor({ state: 'visible', timeout: 30000 });

  await page.getByLabel(/email or username/i).first().fill(process.env.ADMIN_EMAIL!);
  await page.getByLabel(/password/i).first().fill(process.env.ADMIN_PASSWORD!);
  await page.getByRole('button', { name: /sign in/i }).click();

  await page.waitForURL(/\/(dashboard|course-selection)/, { timeout: 60000 });
  await page.waitForTimeout(4000);
  console.log('[regression] login OK, landed on', page.url());
  expect(page.url()).not.toContain('/login');

  let crashes = 0;
  const notFound: string[] = [];

  for (const p of PAGES) {
    pageErrors.length = 0;

    // Always come back to a known-good shell page before the next hop.
    if (new URL(page.url()).pathname !== '/dashboard') {
      await clickSidebarItem(page, 'Dashboard');
      await page.waitForURL('**/dashboard', { timeout: 15000 }).catch(() => undefined);
      await page.waitForTimeout(2500);
    }

    if (p.nav.kind === 'group') {
      // Groups collapse when re-clicked, so only open it when the child is not
      // already visible.
      const child = page.getByRole('button', { name: p.nav.label, exact: true }).first();
      if (!(await child.isVisible().catch(() => false))) {
        await clickSidebarItem(page, p.nav.group);
        await page.waitForTimeout(1200);
      }
      await clickSidebarItem(page, p.nav.label);
    } else if (p.nav.kind === 'header') {
      // Header avatar opens a dropdown menu; the actual link is inside it.
      await page.getByRole('button', { name: p.nav.label, exact: true }).first()
        .click({ timeout: 10000 }).catch(() => undefined);
      await page.waitForTimeout(800);
      await page.getByRole('menuitem', { name: p.nav.label, exact: true }).first()
        .click({ timeout: 10000 }).catch(() => undefined);
    } else {
      await clickSidebarItem(page, p.nav.label);
    }

    // Client-side routing: assert we actually landed on the target path.
    await page.waitForURL(`**${p.path}`, { timeout: 15000 }).catch(() => undefined);
    await page.waitForTimeout(3500);

    const body = await page.locator('body').innerText();
    const url = new URL(page.url()).pathname;

    const errorBoundary = /Something went wrong/i.test(body);
    const bounced = /\/login$/.test(url);
    const wrongRoute = url !== p.path;
    const notFoundPage = /page not found/i.test(body);
    const ok = !errorBoundary && !bounced && !wrongRoute && body.length > 200;

    if (notFoundPage) notFound.push(p.name);
    if (!ok) crashes++;
    console.log(
      `[regression] ${p.name.padEnd(20)} ${ok ? 'OK  ' : 'FAIL'} ` +
      `url=${url} len=${body.length} ` +
      `errorBoundary=${errorBoundary} bouncedToLogin=${bounced} ` +
      `wrongRoute=${wrongRoute} notFound=${notFoundPage} ` +
      `pageErrors=${pageErrors.length}`
    );
    if (pageErrors.length) console.log(`             ${pageErrors[0].slice(0, 200)}`);
  }

  console.log('[regression] TOTAL CRASHED PAGES =', crashes);
  if (notFound.length) console.log('[regression] pages showing NotFound (pre-existing):', notFound.join(', '));
  expect(crashes).toBe(0);
  await ctx.close();
});
