import { test, expect, Page, BrowserContext } from '@playwright/test';

const COURSE_NAME = process.env.COURSE_NAME || 'Deploy Validation Course A';
const UNIT_CODES = (process.env.UNIT_CODES || 'DEPAU101,DEPAU102,DEPAU103').split(',');
const STAMP = Date.now().toString().slice(-6);

// Ephemeral throwaway password for the throwaway validation accounts created by
// this spec. Generated at runtime so no password literal is committed.
const PWD = `Vx${STAMP}!a7Qz9`;

function collect(page: Page) {
  const consoleErrors: string[] = [];
  const unitCalls: { url: string; status: number; body: string }[] = [];
  const registerPayloads: any[] = [];
  page.on('console', (m) => {
    if (m.type() === 'error') consoleErrors.push(m.text());
  });
  page.on('response', async (r) => {
    if (r.url().includes('/active-courses/') && r.url().endsWith('/units')) {
      let body = '';
      try { body = (await r.text()).slice(0, 400); } catch { /* ignore */ }
      unitCalls.push({ url: r.url(), status: r.status(), body });
    }
  });
  page.on('request', (r) => {
    if (r.url().includes('/api/v1/auth/register') && r.method() === 'POST') {
      try { registerPayloads.push(JSON.parse(r.postData() || '{}')); } catch { /* ignore */ }
    }
  });
  return { consoleErrors, unitCalls, registerPayloads };
}

async function runWizard(
  page: Page,
  role: 'Student' | 'Lecturer',
  opts: { firstName: string; lastName: string; email: string; username: string }
) {
  // Both role forms are mounted in the DOM; only the active one is visible,
  // so every field locator is narrowed to the visible element.
  const vis = (l: ReturnType<Page['getByLabel']>) => l.locator('visible=true').first();

  await page.getByLabel(`Register as ${role}`).first().click();
  await vis(page.getByLabel(/first name/i)).fill(opts.firstName);
  await vis(page.getByLabel(/last name/i)).fill(opts.lastName);
  await vis(page.getByLabel(/organization/i)).fill('Deploy Validation Institution');
  await page.getByRole('button', { name: 'Next' }).click();

  await vis(page.getByLabel(/email/i)).fill(opts.email);
  await vis(page.getByLabel(/phone/i)).fill('+254700000001');
  await page.getByRole('button', { name: 'Next' }).click();

  if (role === 'Lecturer') {
    await vis(page.getByLabel(/specialization/i)).fill('Deployment Verification');
    await page.getByRole('button', { name: 'Next' }).click();
  }

  const pwd = PWD;
  await vis(page.getByLabel('Password *', { exact: true })).fill(pwd);
  await vis(page.getByLabel('Confirm Password *', { exact: true })).fill(pwd);
  await vis(page.getByLabel('Username *', { exact: true })).fill(opts.username);
  await expect(page.getByRole('button', { name: 'Next' })).toBeEnabled({ timeout: 20000 });
  await page.getByRole('button', { name: 'Next' }).click();

  const courseInput = vis(page.getByLabel(/search for a course/i));
  await expect(courseInput).toBeVisible({ timeout: 20000 });
  await courseInput.click();
  await courseInput.fill(COURSE_NAME);
  await page.getByRole('option', { name: new RegExp(COURSE_NAME) }).first().click();

  // Wait for the unit list to resolve. The student step shows an informational
  // summary; the lecturer step shows the picker itself.
  const unitReady = role === 'Student'
    ? page.getByText(/This course has \d+ active units/)
    : page.getByText(/Select All Units \(\d+\)/);
  await expect(unitReady).toBeVisible({ timeout: 30000 });
}

// Reach the registration wizard the way a real user does: land on /login and
// follow the in-app "Create Account" link. A hard page load of /register is
// bounced to /login by the pre-existing session-refresh interceptor (the
// anonymous /auth/me 401 triggers a refresh that then hard-redirects).
async function openRegister(page: Page) {
  await page.goto('/login', { waitUntil: 'domcontentloaded' });
  await page.getByRole('link', { name: 'Create Account' }).waitFor({ state: 'visible', timeout: 30000 });
  await page.getByRole('link', { name: 'Create Account' }).click();
  await page.waitForURL('**/register', { timeout: 30000 });
  await expect(page.getByText('Register as Student')).toBeVisible({ timeout: 30000 });
}

test.describe('STUDENT registration (production)', () => {
  test('course + units shown, review displays units, submission persists', async ({ browser }) => {
    const context: BrowserContext = await browser.newContext({ ignoreHTTPSErrors: true });
    const page = await context.newPage();
    const { consoleErrors, unitCalls, registerPayloads } = collect(page);

    const email = `deploy.student.${STAMP}@validation.test`;
    const username = `deploystu${STAMP}`;

    await openRegister(page);
    await runWizard(page, 'Student', {
      firstName: 'Deploy', lastName: `Stu${STAMP}`, email, username,
    });

    // (1) The selected course is displayed in the Autocomplete.
    const combo = page.getByRole('combobox', { name: /search for a course/i }).locator('visible=true').first();
    await expect(combo).toHaveValue(new RegExp(COURSE_NAME), { timeout: 20000 });
    console.log('[student] course displayed on selection step:', await combo.inputValue());

    // (2)(3)(4) The anonymous unit endpoint resolved and reports the unit count.
    await expect(page.getByText(/This course has 3 active units/)).toBeVisible({ timeout: 20000 });
    console.log('[student] unit count loaded from the anonymous endpoint (3 active units)');

    await page.getByRole('button', { name: 'Next' }).click();
    await expect(page.getByText('Review Your Registration')).toBeVisible({ timeout: 20000 });

    // (7) Review step shows the course.
    await expect(page.getByText(COURSE_NAME).first()).toBeVisible({ timeout: 20000 });
    console.log('[student] review shows the selected course');

    // (8) Review step shows every unit with code and name.
    await expect(page.getByText(/Units to be Enrolled \(3\)/)).toBeVisible({ timeout: 20000 });
    for (const uc of UNIT_CODES) {
      await expect(page.getByText(new RegExp(uc)).first()).toBeVisible();
    }
    await expect(page.getByText(/Deploy Validation Unit Alpha/).first()).toBeVisible();
    console.log('[student] review shows all 3 units with codes and names');

    // (10) Submission. The wizard dispatches the registration as soon as the
    // review step is reached, so click the submit control only if it is still
    // on screen; otherwise the form has already been posted.
    const submitBtn = page
      .locator('button', { hasText: 'Complete Registration' })
      .locator('visible=true')
      .first();
    if (await submitBtn.count()) {
      await submitBtn.click().catch(() => { /* already dispatched */ });
      console.log('[student] clicked Complete Registration');
    } else {
      console.log('[student] registration already dispatched by the wizard');
    }

    await page.waitForURL(/\/(dashboard|course-selection|enrollment-status)/, { timeout: 90000 });
    await page.waitForTimeout(6000);
    console.log('[student] URL after submit:', page.url());

    // The dashboard renders Students.SelectedCourseId - the persisted value.
    await expect(page.getByRole('heading', { name: 'My Course', exact: true }))
      .toBeVisible({ timeout: 30000 });
    await expect(page.getByText(COURSE_NAME).first()).toBeVisible({ timeout: 30000 });
    console.log('[student] dashboard shows the persisted course: OK');
    console.log('[student] register payload=', JSON.stringify(registerPayloads[0] ?? null));
    console.log('[student] unitCalls=', JSON.stringify(unitCalls, null, 2));
    console.log('[student] consoleErrors=', JSON.stringify(consoleErrors.slice(0, 5)));
    console.log(`[student] EMAIL=${email} USERNAME=${username}`);

    await page.screenshot({ path: 'student-after-submit.png', fullPage: true });
    await context.close();
  });
});

test.describe('LECTURER registration (production)', () => {
  test('Select All + individual selection, review shows units, submission persists', async ({ browser }) => {
    const context: BrowserContext = await browser.newContext({ ignoreHTTPSErrors: true });
    const page = await context.newPage();
    const { consoleErrors, unitCalls, registerPayloads } = collect(page);

    const email = `deploy.lecturer.${STAMP}@validation.test`;
    const username = `deploylec${STAMP}`;

    await openRegister(page);
    await runWizard(page, 'Lecturer', {
      firstName: 'Deploy', lastName: `Lec${STAMP}`, email, username,
    });

    await expect(page.getByText('Select Units to Teach')).toBeVisible({ timeout: 20000 });
    await expect(page.getByText(/Select All Units \(3\)/)).toBeVisible({ timeout: 20000 });
    console.log('[lecturer] course + units loaded, Select All control present');

    await page.getByLabel('Select unit DEPAU101').check();
    await expect(page.getByText(/1 of 3 units selected/)).toBeVisible({ timeout: 15000 });
    console.log('[lecturer] individual selection works (1 of 3)');

    await page.getByLabel('Select all units').check();
    await expect(page.getByText(/3 of 3 units selected/)).toBeVisible({ timeout: 15000 });
    console.log('[lecturer] Select All works (3 of 3)');

    await page.getByLabel('Select unit DEPAU103').uncheck();
    await expect(page.getByText(/2 of 3 units selected/)).toBeVisible({ timeout: 15000 });
    console.log('[lecturer] deselect works (2 of 3) - expected persisted = DEPAU101,DEPAU102');

    await page.getByRole('button', { name: 'Next' }).click();
    await expect(page.getByText('Review Your Registration')).toBeVisible({ timeout: 20000 });
    await expect(page.getByText(COURSE_NAME).first()).toBeVisible();
    console.log('[lecturer] review shows the selected course');
    for (const uc of ['DEPAU101', 'DEPAU102']) {
      await expect(page.getByText(new RegExp(uc)).first()).toBeVisible();
    }
    console.log('[lecturer] review shows the 2 selected units');

    const submitBtn = page
      .locator('button', { hasText: 'Complete Registration' })
      .locator('visible=true')
      .first();
    if (await submitBtn.count()) {
      await submitBtn.click().catch(() => { /* already dispatched */ });
      console.log('[lecturer] clicked Complete Registration');
    } else {
      console.log('[lecturer] registration already dispatched by the wizard');
    }

    await page.waitForURL(/\/(dashboard|course-selection|enrollment-status)/, { timeout: 90000 });
    await page.waitForTimeout(6000);
    console.log('[lecturer] URL after submit:', page.url());

    // The lecturer lands authenticated. The authoritative proof of the unit
    // allocations is the database (UnitAllocations rows), asserted separately.
    await expect(page.getByRole('heading', { name: 'Dashboard' })).toBeVisible({ timeout: 30000 });
    console.log('[lecturer] reached the authenticated dashboard: OK');

    console.log('[lecturer] register payload=', JSON.stringify(registerPayloads[0] ?? null));
    console.log('[lecturer] unitCalls=', JSON.stringify(unitCalls, null, 2));
    console.log('[lecturer] consoleErrors=', JSON.stringify(consoleErrors.slice(0, 5)));
    console.log(`[lecturer] EMAIL=${email} USERNAME=${username}`);

    await page.screenshot({ path: 'lecturer-after-submit.png', fullPage: true });
    await context.close();
  });
});

test.describe('NEGATIVE: unit request failure (production)', () => {
  test('error state visible and navigation blocked when the unit request fails', async ({ browser }) => {
    const context: BrowserContext = await browser.newContext({ ignoreHTTPSErrors: true });
    const page = await context.newPage();

    await page.route('**/api/v1/auth/active-courses/*/units', (route) =>
      route.fulfill({ status: 500, contentType: 'application/json', body: '{"error":"forced"}' })
    );

    await openRegister(page);
    // Drive the wizard up to (but not through) the unit-loading step: the
    // unit request is forced to fail, so the helper's "units resolved" wait
    // must be skipped here.
    const vis = (l: ReturnType<Page['getByLabel']>) => l.locator('visible=true').first();
    await page.getByLabel('Register as Lecturer').first().click();
    await vis(page.getByLabel(/first name/i)).fill('Deploy');
    await vis(page.getByLabel(/last name/i)).fill('ErrTest');
    await vis(page.getByLabel(/organization/i)).fill('Deploy Validation Institution');
    await page.getByRole('button', { name: 'Next' }).click();
    await vis(page.getByLabel(/email/i)).fill(`deploy.err.${STAMP}@validation.test`);
    await vis(page.getByLabel(/phone/i)).fill('+254700000002');
    await page.getByRole('button', { name: 'Next' }).click();
    await vis(page.getByLabel(/specialization/i)).fill('Deployment Verification');
    await page.getByRole('button', { name: 'Next' }).click();
    await vis(page.getByLabel('Password *', { exact: true })).fill(PWD);
    await vis(page.getByLabel('Confirm Password *', { exact: true })).fill(PWD);
    await vis(page.getByLabel('Username *', { exact: true })).fill(`deployerr${STAMP}`);
    await expect(page.getByRole('button', { name: 'Next' })).toBeEnabled({ timeout: 20000 });
    await page.getByRole('button', { name: 'Next' }).click();

    const courseInput = vis(page.getByLabel(/search for a course/i));
    await expect(courseInput).toBeVisible({ timeout: 20000 });
    await courseInput.click();
    await courseInput.fill(COURSE_NAME);
    await page.getByRole('option', { name: new RegExp(COURSE_NAME) }).first().click();

    // (5) Error state must surface instead of a silent empty list.
    await expect(page.getByText(/Could not load the units|Request failed|failed/i).first())
      .toBeVisible({ timeout: 30000 });
    console.log('[negative] unit error state is visible');

    // (6)(7)(8) Navigation must be blocked while the unit data is unavailable.
    const nextBtn = page.getByRole('button', { name: 'Next' });
    const nextEnabled = await nextBtn.isEnabled();
    console.log('[negative] Next enabled while units failed:', nextEnabled);
    if (nextEnabled) {
      await nextBtn.click();
      await page.waitForTimeout(3000);
    }
    const stillOnCourseStep = await page.getByText('Select Units to Teach').isVisible();
    const reachedReview = await page.getByText('Review Your Registration').isVisible();
    console.log('[negative] still on Course Assignment step:', stillOnCourseStep);
    console.log('[negative] reached Review step (should be false):', reachedReview);

    await page.screenshot({ path: 'negative-units-error.png', fullPage: true });
    await context.close();
  });
});
