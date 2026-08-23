import { test, expect } from '../tests/fixtures/apiFixtures';

test.describe('Admin Dashboard & Management', () => {
  test('health endpoint accessible without auth', async ({ request }) => {
    const response = await request.get('/health');
    expect(response.status()).toBe(200);
  });

  test('students endpoint requires auth', async ({ request }) => {
    const response = await request.get('/api/v1/students');
    expect(response.status()).toBe(401);
  });

  test('students endpoint accessible with admin token', async ({ adminToken, request }) => {
    const response = await request.get('/api/v1/students?page=1&pageSize=10', {
      headers: {
        'Authorization': `Bearer ${adminToken}`,
        'X-Tenant-Id': '11111111-1111-1111-1111-111111111111'
      }
    });
    expect(response.status()).toBe(200);
  });

  test('student CRUD flow', async ({ adminToken, request }) => {
    const headers = {
      'Authorization': `Bearer ${adminToken}`,
      'X-Tenant-Id': '11111111-1111-1111-1111-111111111111'
    };

    // Create
    const email = `e2e.crud.${Date.now()}@example.com`;
    const createResponse = await request.post('/api/v1/students', {
      headers,
      data: {
        firstName: 'E2E-CRUD',
        lastName: 'Student',
        email,
        phoneNumber: '+254712345678',
        dateOfBirth: '2000-01-01T00:00:00Z',
        gender: 'Female',
        address: '123 E2E Test St'
      }
    });
    expect(createResponse.status()).toBe(201);
    const student = await createResponse.json();

    // Read
    const getResponse = await request.get(`/api/v1/students/${student.id}`, { headers });
    expect(getResponse.status()).toBe(200);

    // Delete
    const deleteResponse = await request.delete(`/api/v1/students/${student.id}`, { headers });
    expect(deleteResponse.status()).toBe(204);
  });
});

test.describe('Course Offering Workflow', () => {
  test('course offerings require auth', async ({ request }) => {
    const response = await request.get('/api/v1/courseoffering');
    expect(response.status()).toBe(401);
  });
});

test.describe('Security Headers', () => {
  test('response includes security headers', async ({ request }) => {
    const response = await request.get('/api/v1/auth/login', {
      data: { email: '', password: '' }
    });
    const headers = response.headers();
    expect(headers['x-content-type-options'] || '').toBe('nosniff');
  });
});