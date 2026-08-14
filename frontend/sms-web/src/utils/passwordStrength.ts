/**
 * Password strength engine for the School Management System.
 *
 * Implements an entropy-based scoring algorithm (Shannon entropy via
 * character-pool size), a live requirements checklist, weak-password
 * blacklist detection, and an optional Have I Been Pwned (HIBP)
 * k-Anonymity breach check with graceful offline fallback.
 *
 * Security notes:
 *  - Passwords are NEVER logged or persisted to localStorage.
 *  - The HIBP check only sends the first 5 hex chars of the SHA-1 hash
 *    (k-Anonymity); the full password/hash never leaves the client.
 */

export type PasswordLevel = 'Weak' | 'Medium' | 'Strong' | 'Very Strong';

export interface PasswordRequirements {
  minLength: boolean; // >= 8
  hasUpper: boolean; // A-Z
  hasLower: boolean; // a-z
  hasNumber: boolean; // 0-9
  hasSpecial: boolean; // supported special chars
  min12: boolean; // recommended 12+
}

export interface PasswordContext {
  email?: string;
  username?: string;
  firstName?: string;
  lastName?: string;
  organization?: string;
  schoolName?: string;
}

export interface PasswordStrengthResult {
  score: number; // 0-100
  level: PasswordLevel;
  entropy: number; // estimated bits
  requirements: PasswordRequirements;
  blacklistHits: string[];
  breached: boolean;
}

const SPECIAL_CHAR_REGEX = /[!@#$%^&*()_+\-=[\]{};':"\\|,.<>/?~`]/;
const UPPER_REGEX = /[A-Z]/;
const LOWER_REGEX = /[a-z]/;
const NUMBER_REGEX = /[0-9]/;

// Common blacklist (offline fallback for deployments without internet).
// These are also checked against the HIBP API when available.
const COMMON_BLACKLIST = [
  'password',
  'admin',
  'qwerty',
  '12345678',
  'abc123',
  'letmein',
  'welcome',
  'monkey',
  'dragon',
  'football',
  'baseball',
  'iloveyou',
  'trustno1',
  'sunshine',
  'princess',
  'master',
  'login',
  'passw0rd',
  '123456789',
  '1234567890',
  'password1',
  'qwerty123',
  '11111111',
];

// Keyboard-row patterns that are trivially guessable.
const KEYBOARD_PATTERNS = [
  'qwerty',
  'qwertyuiop',
  'asdfghjkl',
  'zxcvbnm',
  '123456',
  '1234567',
  '12345678',
  '123456789',
  '1234567890',
  'abcdef',
  'abcdefg',
  'poiuyt',
  'mnbvcxz',
];

const DEFAULT_SCHOOL_NAME = 'schoolmanagement';

/**
 * Estimate password entropy in bits using the character-pool model:
 *   entropy = length * log2(poolSize)
 * where poolSize is the size of the character set actually used.
 */
export function calculateEntropy(password: string): number {
  if (!password) return 0;
  let poolSize = 0;
  if (LOWER_REGEX.test(password)) poolSize += 26;
  if (UPPER_REGEX.test(password)) poolSize += 26;
  if (NUMBER_REGEX.test(password)) poolSize += 10;
  if (SPECIAL_CHAR_REGEX.test(password)) poolSize += 33;
  if (poolSize === 0) return 0;
  return password.length * Math.log2(poolSize);
}

/**
 * Evaluate each password requirement against the current value.
 */
export function validatePasswordRequirements(password: string): PasswordRequirements {
  return {
    minLength: password.length >= 8,
    hasUpper: UPPER_REGEX.test(password),
    hasLower: LOWER_REGEX.test(password),
    hasNumber: NUMBER_REGEX.test(password),
    hasSpecial: SPECIAL_CHAR_REGEX.test(password),
    min12: password.length >= 12,
  };
}

/**
 * Detect repeated sequences (e.g. "aaaa", "ababab", "123123") and
 * keyboard-row patterns (e.g. "qwerty", "asdfgh").
 */
function detectPatternFlags(password: string): { repeated: boolean; keyboard: boolean } {
  const lower = password.toLowerCase();
  let repeated = false;
  let keyboard = false;

  // Keyboard patterns
  for (const pattern of KEYBOARD_PATTERNS) {
    if (lower.includes(pattern)) {
      keyboard = true;
      break;
    }
  }

  // Repeated sequences: look for a substring of length >= 2 that appears
  // consecutively at least twice (e.g. "abab", "123123", "aaaa").
  for (let len = 2; len <= Math.floor(lower.length / 2) && !repeated; len++) {
    for (let i = 0; i + len * 2 <= lower.length && !repeated; i++) {
      const first = lower.substring(i, i + len);
      const second = lower.substring(i + len, i + len * 2);
      if (first === second) {
        repeated = true;
      }
    }
  }

  return { repeated, keyboard };
}

/**
 * Check the local blacklist: common passwords, the school name, and
 * user-specific values (email local part, username, first/last name,
 * organization). Returns a list of human-readable reasons.
 */
export function checkBlacklist(password: string, context?: PasswordContext): string[] {
  const hits: string[] = [];
  const lower = password.toLowerCase();

  // Common passwords — check both exact match and substring containment so
  // e.g. "Password123!" is caught because it contains "password".
  for (const bad of COMMON_BLACKLIST) {
    if (lower === bad || lower.includes(bad)) {
      hits.push('This password is too common and easy to guess');
      break;
    }
  }

  // Substring containment for keyboard/repeated patterns
  const { repeated, keyboard } = detectPatternFlags(lower);
  if (repeated) hits.push('Password contains repeated sequences');
  if (keyboard) hits.push('Password contains an easy keyboard pattern');

  // School name
  const organization = context?.organization?.toLowerCase() || '';
  const schoolName = context?.schoolName?.toLowerCase() || DEFAULT_SCHOOL_NAME;
  const schoolTokens = [schoolName, 'school', 'management', 'system', 'sms'];
  if (schoolTokens.some((t) => t && lower.includes(t))) {
    hits.push('Password must not contain the school name');
  }

  // User-specific values
  const userValues = [
    context?.email?.toLowerCase(),
    context?.username?.toLowerCase(),
    context?.firstName?.toLowerCase(),
    context?.lastName?.toLowerCase(),
    organization,
  ].filter(Boolean) as string[];

  for (const value of userValues) {
    // Ignore very short values (e.g. single letters) to avoid false positives.
    if (value.length >= 3 && lower.includes(value)) {
      hits.push('Password must not contain your personal information');
      break;
    }
  }

  return hits;
}

/**
 * Map a password to a strength level per the product specification.
 *
 *  - Weak:        < 8 chars OR missing multiple requirements
 *  - Medium:      meets min length + upper + lower + special, missing number
 *  - Strong:      meets all required rules, 8-11 chars
 *  - Very Strong: meets all required rules, 12+ chars, no repeated/pattern/
 *                 dictionary words, high entropy
 */
export function getPasswordStrength(password: string, context?: PasswordContext): PasswordStrengthResult {
  const requirements = validatePasswordRequirements(password);
  const entropy = calculateEntropy(password);
  const blacklistHits = checkBlacklist(password, context);
  const { repeated, keyboard } = detectPatternFlags(password);

  const allRequired =
    requirements.minLength &&
    requirements.hasUpper &&
    requirements.hasLower &&
    requirements.hasNumber &&
    requirements.hasSpecial;

  let level: PasswordLevel;
  let score: number;

  if (!password) {
    level = 'Weak';
    score = 0;
  } else if (!allRequired) {
    // Missing one or more required rules.
    level = 'Weak';
    // Score reflects how many requirements are met.
    const met = [requirements.minLength, requirements.hasUpper, requirements.hasLower, requirements.hasNumber, requirements.hasSpecial].filter(Boolean).length;
    score = Math.round((met / 5) * 40);
  } else if (blacklistHits.length > 0 || repeated || keyboard) {
    // Meets rules but is a known/pattern password → cap at Medium.
    level = 'Medium';
    score = 55;
  } else if (password.length < 12) {
    // All rules met, 8-11 chars → Strong.
    level = 'Strong';
    score = 75;
  } else {
    // All rules met, 12+ chars, high entropy → Very Strong.
    level = 'Very Strong';
    score = 95;
  }

  // Entropy-based refinement: Very Strong also requires high entropy.
  if (level === 'Very Strong' && entropy < 70) {
    level = 'Strong';
    score = 80;
  }

  return {
    score,
    level,
    entropy: Math.round(entropy),
    requirements,
    blacklistHits,
    breached: false,
  };
}

/**
 * Check whether a password has appeared in known data breaches using
 * Have I Been Pwned's k-Anonymity API.
 *
 * Only the first 5 hex chars of the SHA-1 hash are sent to the API; the
 * password and full hash never leave the client. If the network is
 * unavailable or the API errors, we fail closed (treat as not-breached)
 * so the offline/local blacklist still applies.
 */
export async function checkBreachedPassword(password: string): Promise<boolean> {
  try {
    const hash = await sha1Hex(password);
    const prefix = hash.substring(0, 5).toUpperCase();
    const suffix = hash.substring(5).toUpperCase();

    const response = await fetch(`https://api.pwnedpasswords.com/range/${prefix}`);
    if (!response.ok) return false;

    const text = await response.text();
    // Response is a list of "SUFFIX:COUNT" lines.
    return text
      .split('\n')
      .some((line) => line.split(':')[0].trim().toUpperCase() === suffix);
  } catch {
    // Offline or API unreachable — rely on the local blacklist.
    return false;
  }
}

/**
 * Compute the SHA-1 hex digest of a string using the Web Crypto API.
 * Falls back to a pure-JS implementation if crypto.subtle is unavailable.
 */
async function sha1Hex(input: string): Promise<string> {
  if (typeof crypto !== 'undefined' && crypto.subtle) {
    const data = new TextEncoder().encode(input);
    const digest = await crypto.subtle.digest('SHA-1', data);
    return Array.from(new Uint8Array(digest))
      .map((b) => b.toString(16).padStart(2, '0'))
      .join('');
  }

  // Pure-JS fallback (for older environments / tests).
  return jsSha1(input);
}

/**
 * Minimal pure-JS SHA-1 implementation (used only when WebCrypto is absent).
 */
function jsSha1(input: string): string {
  function rotl(n: number, b: number): number {
    return (n << b) | (n >>> (32 - b));
  }
  function toHexStr(n: number): string {
    let s = '';
    let v: number;
    for (let i = 7; i >= 0; i--) {
      v = (n >>> (i * 4)) & 0x0f;
      s += v.toString(16);
    }
    return s;
  }

  const utf8 = unescape(encodeURIComponent(input));
  const words: number[] = [];
  for (let i = 0; i < utf8.length * 8; i += 8) {
    words[i >> 5] |= (utf8.charCodeAt(i / 8) & 0xff) << (24 - (i % 32));
  }
  words[utf8.length >> 5] |= 0x80 << (24 - (utf8.length % 32) * 8);
  words[((utf8.length + 64 >> 9) << 4) + 15] = utf8.length * 8;

  let a = 0x67452301;
  let b = 0xefcdab89;
  let c = 0x98badcfe;
  let d = 0x10325476;
  let e = 0xc3d2e1f0;

  for (let i = 0; i < words.length; i += 16) {
    const w = new Array(80);
    for (let j = 0; j < 16; j++) w[j] = words[i + j];
    for (let j = 16; j < 80; j++) {
      w[j] = rotl(w[j - 3] ^ w[j - 8] ^ w[j - 14] ^ w[j - 16], 1);
    }

    let A = a;
    let B = b;
    let C = c;
    let D = d;
    let E = e;

    for (let j = 0; j < 80; j++) {
      let f: number;
      let k: number;
      if (j < 20) {
        f = (B & C) | (~B & D);
        k = 0x5a827999;
      } else if (j < 40) {
        f = B ^ C ^ D;
        k = 0x6ed9eba1;
      } else if (j < 60) {
        f = (B & C) | (B & D) | (C & D);
        k = 0x8f1bbcdc;
      } else {
        f = B ^ C ^ D;
        k = 0xca62c1d6;
      }

      const temp = (rotl(A, 5) + f + E + k + w[j]) | 0;
      E = D;
      D = C;
      C = rotl(B, 30);
      B = A;
      A = temp;
    }

    a = (a + A) | 0;
    b = (b + B) | 0;
    c = (c + C) | 0;
    d = (d + D) | 0;
    e = (e + E) | 0;
  }

  return toHexStr(a) + toHexStr(b) + toHexStr(c) + toHexStr(d) + toHexStr(e);
}
