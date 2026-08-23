import { test, expect } from '../tests/fixtures/apiFixtures';

test.describe('Authentication Flow', () => {
  test('successful login returns tokens via cookies', async ({ request }) => {
    const response = await request.post('/api/v1/auth/login', {
      data: {
        email: 'admin@school.com',
        password: 'Admin123!@#q1',
        rememberMe: true
      }
    });
    expect(response.status()).toBe(200);

    const cookies = response.headers()['set-cookie'] || '';
    expect(cookies).toContain('access_token');
    expect(cookies).toContain('refresh_token');
    expect(cookies).toContain('HttpOnly');
  });

  test('invalid credentials returns 401', async ({ request }) => {
    const response = await request.post('/api/v1/auth/login', {
      data: {
        email: 'admin@school.com',
        password: 'wrong-password',
        rememberMe: true
      }
    });
    expect(response.status()).toBe(401);
  });

  test('non-existent user returns 401', async ({ request }) => {
    const response = await request.post('/api/v1/auth/login', {
      data: {
        email: 'nonexistent@school.com',
        password: 'SomePassword123!',
        rememberMe: true
      }
    });
    expect(response.status()).toBe(401);
  });

  test('registration creates new user', async ({ request }) => {
    const email = `e2e.test.${Date.now()}@example.com`;
    const response = await request.post('/api/v1/auth/register', {
      data: {
        firstName: 'E2E',
        lastName: 'Test',
        email,
        password: 'Str0ng!@#Pass789',
        confirmPassword: 'Str0ng!@#Pass789',
        phoneNumber: '+254712345678',
        organization: 'E2E Test School',
        role: 'Student',
        courseId: '22222222-2222-2222-2222-222222222222'
      }
    });
    expect(response.status()).toBe(201);
    const body = await response.json();
    expect(body.email).toBe(email);
  });

  test('health endpoint returns healthy', async ({ request }) => {
    const response = await request.get('/health');
    expect(response.status()).toBe(200);
    const body = await response.json();
    expect(body.status).toBe('Healthy');
  });
});