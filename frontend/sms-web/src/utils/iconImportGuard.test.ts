import { describe, it, expect } from 'vitest';

/**
 * Regression guard for the production OMS crash.
 *
 * Production symptom: clicking "Order Management / Requests / Request Workspace"
 * rendered the app-wide ErrorBoundary fallback — "Something went wrong. We
 * couldn't complete your request." — with React throwing
 *
 *   "Minified React error #130 ... got: object"
 *
 * Cause: `import X from '@mui/icons-material/<Icon>'` (a deep DEFAULT import).
 * @mui/icons-material ships no `exports` map and those deep entry points are
 * CommonJS, so the production bundle binds X to the module namespace OBJECT
 * instead of the component. React then rejects it as an element type.
 *
 * Why this needs a static guard: component/unit tests import the package
 * through its ESM barrel, where the same specifier resolves to a real
 * component. That is exactly why 147 passing OMS unit tests coexisted with a
 * hard production crash. The defect only exists in the built output, so the
 * invariant has to be asserted on the SOURCE TEXT.
 *
 * Sources are read through Vite's `import.meta.glob` (?raw) so the guard needs
 * no Node typings and no new dependency.
 */

const sources = import.meta.glob('../**/*.{ts,tsx}', {
  query: '?raw',
  import: 'default',
  eager: true,
}) as Record<string, string>;

const DEEP_ICON_IMPORT = /from\s+['"]@mui\/icons-material\//;

/** Strips comments so the explanatory notes in the fixed files never trip the guard. */
const stripComments = (code: string): string =>
  code.replace(/\/\*[\s\S]*?\*\//g, '').replace(/\/\/.*$/gm, '');

function offenders(): string[] {
  const found: string[] = [];
  for (const [file, raw] of Object.entries(sources)) {
    stripComments(raw)
      .split('\n')
      .forEach((line, i) => {
        if (DEEP_ICON_IMPORT.test(line)) found.push(`${file}:${i + 1}: ${line.trim()}`);
      });
  }
  return found;
}

describe('MUI icon import regression guard', () => {
  it('scans the frontend source tree', () => {
    expect(Object.keys(sources).length).toBeGreaterThan(50);
  });

  it('no source file uses a deep default import of an @mui/icons-material module', () => {
    const found = offenders();
    expect(
      found,
      `Deep default icon imports break the production bundle (React error #130):\n${found.join('\n')}`,
    ).toEqual([]);
  });

  it('the previously crashing OMS request pages import icons from the barrel', () => {
    // These are the exact files that took the production page down.
    const crashing = [
      'pages/oms/RequestsDashboard.tsx',
      'pages/oms/RequestsList.tsx',
      'pages/oms/RequestDetailPage.tsx',
      'pages/oms/CreateRequestPage.tsx',
      'components/omsRequests/RequestStatusHistoryPanel.tsx',
      'components/omsRequests/RequestCommentsPanel.tsx',
      'components/omsRequests/RequestAttachmentsPanel.tsx',
    ];

    for (const rel of crashing) {
      const key = Object.keys(sources).find((k) => k.endsWith(`/${rel}`) || k.endsWith(rel));
      expect(key, `${rel} should exist`).toBeDefined();
      expect(
        DEEP_ICON_IMPORT.test(stripComments(sources[key!])),
        `${rel} must import MUI icons from the '@mui/icons-material' barrel`,
      ).toBe(false);
    }
  });
});
