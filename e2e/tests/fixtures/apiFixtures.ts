import { test as base } from '@playwright/test';
import { join } from 'path';

export interface TestFixtures {
  adminToken: string;
  apiBaseUrl: string;
}

export const test = base.extend<TestFixtures>({
  adminToken: async ({ request }, use) => {
    const response = await request.post('/api/v1/auth/login', {
      data: {
        email: 'admin@school.com',
        password: 'Admin123!@#q1',
        rememberMe: true
      }
    });
    const cookies = response.headers()['set-cookie'] || '';
    const token = extractCookie(cookies, 'access_token');
    await use(token);
  },
  apiBaseUrl: async ({ }, use) => {
    await use(process.env.BASE_URL || 'http://localhost:5000');
  },
});

function extractCookie(cookieHeader: string, name: string): string {
  const parts = cookieHeader.split(';');
  for (const part of parts) {
    const eq = part.indexOf('=');
    if (eq > 0 && part.substring(0, eq).trim() === name) {
      return part.substring(eq + 1).trim();
    }
  }
  return '';
}

export { expect } from '@playwright/test';