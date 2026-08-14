import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import {
  calculateEntropy,
  validatePasswordRequirements,
  checkBlacklist,
  getPasswordStrength,
  checkBreachedPassword,
} from './passwordStrength';

describe('calculateEntropy', () => {
  it('returns 0 for empty string', () => {
    expect(calculateEntropy('')).toBe(0);
  });

  it('computes entropy for lowercase-only password', () => {
    // 8 lowercase chars: 8 * log2(26) ≈ 37.6
    const entropy = calculateEntropy('abcdefgh');
    expect(entropy).toBeGreaterThan(37);
    expect(entropy).toBeLessThan(38);
  });

  it('computes higher entropy for mixed character sets', () => {
    const lower = calculateEntropy('abcdefgh');
    const mixed = calculateEntropy('Abcdefg1!');
    expect(mixed).toBeGreaterThan(lower);
  });
});

describe('validatePasswordRequirements', () => {
  it('returns all false for empty password', () => {
    const req = validatePasswordRequirements('');
    expect(req.minLength).toBe(false);
    expect(req.hasUpper).toBe(false);
    expect(req.hasLower).toBe(false);
    expect(req.hasNumber).toBe(false);
    expect(req.hasSpecial).toBe(false);
    expect(req.min12).toBe(false);
  });

  it('detects all required character types', () => {
    const req = validatePasswordRequirements('Abcdef123!');
    expect(req.minLength).toBe(true);
    expect(req.hasUpper).toBe(true);
    expect(req.hasLower).toBe(true);
    expect(req.hasNumber).toBe(true);
    expect(req.hasSpecial).toBe(true);
    expect(req.min12).toBe(false);
  });

  it('detects 12+ length', () => {
    const req = validatePasswordRequirements('Abcdefgh123!');
    expect(req.minLength).toBe(true);
    expect(req.min12).toBe(true);
  });
});

describe('checkBlacklist', () => {
  it('rejects common passwords', () => {
    expect(checkBlacklist('password').length).toBeGreaterThan(0);
    expect(checkBlacklist('qwerty123').length).toBeGreaterThan(0);
    expect(checkBlacklist('12345678').length).toBeGreaterThan(0);
  });

  it('rejects school name in password', () => {
    const hits = checkBlacklist('SchoolManagement1!', { schoolName: 'schoolmanagement' });
    expect(hits.length).toBeGreaterThan(0);
  });

  it('rejects personal information', () => {
    const hits = checkBlacklist('JohnDoe123!', {
      firstName: 'John',
      lastName: 'Doe',
    });
    expect(hits.length).toBeGreaterThan(0);
  });

  it('rejects repeated sequences', () => {
    expect(checkBlacklist('aaaa1234!').length).toBeGreaterThan(0);
    expect(checkBlacklist('ababab12!').length).toBeGreaterThan(0);
  });

  it('rejects keyboard patterns', () => {
    expect(checkBlacklist('qwerty123!').length).toBeGreaterThan(0);
  });

  it('allows strong passwords with no hits', () => {
    const hits = checkBlacklist('Xk9#mQ2$vL7!', {
      firstName: 'John',
      lastName: 'Doe',
      email: 'john.doe@example.com',
    });
    expect(hits).toEqual([]);
  });
});

describe('getPasswordStrength', () => {
  it('returns Weak for empty password', () => {
    const result = getPasswordStrength('');
    expect(result.level).toBe('Weak');
    expect(result.score).toBe(0);
  });

  it('returns Weak when missing required rules', () => {
    const result = getPasswordStrength('short');
    expect(result.level).toBe('Weak');
  });

  it('returns Medium for known/blacklisted password meeting rules', () => {
    const result = getPasswordStrength('Password123!');
    expect(result.level).toBe('Medium');
  });

  it('returns Strong for 8-11 char password meeting all rules', () => {
    const result = getPasswordStrength('Xk9#mQ2$v');
    expect(result.level).toBe('Strong');
  });

  it('returns Very Strong for 12+ char password with high entropy', () => {
    const result = getPasswordStrength('Xk9#mQ2$vL7!rT');
    expect(result.level).toBe('Very Strong');
    expect(result.score).toBeGreaterThanOrEqual(90);
  });

  it('includes requirements and blacklist info', () => {
    const result = getPasswordStrength('Xk9#mQ2$vL7!');
    expect(result.requirements.minLength).toBe(true);
    expect(result.requirements.hasUpper).toBe(true);
    expect(result.blacklistHits).toEqual([]);
  });
});

describe('checkBreachedPassword', () => {
  let fetchMock: ReturnType<typeof vi.fn>;

  beforeEach(() => {
    fetchMock = vi.fn();
    vi.stubGlobal('fetch', fetchMock);
  });

  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it('returns false when API unavailable (graceful offline fallback)', async () => {
    fetchMock.mockRejectedValue(new Error('network error'));
    const result = await checkBreachedPassword('Xk9#mQ2$vL7!');
    expect(result).toBe(false);
  });

  it('returns false when API returns non-OK', async () => {
    fetchMock.mockResolvedValue({ ok: false });
    const result = await checkBreachedPassword('Xk9#mQ2$vL7!');
    expect(result).toBe(false);
  });

  it('returns true when password hash suffix is found', async () => {
    // SHA-1 of 'password123' is 'CBFDAC6008F9CAB4083784CBD1874F76618D2A97'
    // prefix = 'CBFDA', suffix = 'C6008F9CAB4083784CBD1874F76618D2A97'
    fetchMock.mockResolvedValue({
      ok: true,
      text: async () => 'C6008F9CAB4083784CBD1874F76618D2A97:12345\n0000000000000000000000000000000000000000:1\n',
    });
    const result = await checkBreachedPassword('password123');
    expect(result).toBe(true);
  });

  it('returns false when suffix not found', async () => {
    fetchMock.mockResolvedValue({
      ok: true,
      text: async () => '0000000000000000000000000000000000000000:1\n',
    });
    const result = await checkBreachedPassword('Xk9#mQ2$vL7!');
    expect(result).toBe(false);
  });
});
