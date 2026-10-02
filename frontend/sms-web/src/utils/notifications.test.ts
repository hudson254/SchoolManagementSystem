import { describe, it, expect } from 'vitest';
import type { Notification } from '../services/notification.service';
import {
  formatNotificationDate,
  getNotificationActionLabel,
  getNotificationActionUrl,
  getNotificationVisuals,
  normalizePriority,
  priorityRank,
} from './notifications';

const make = (overrides: Partial<Notification> = {}): Notification => ({
  id: 'n1',
  title: 'Title',
  message: 'Message',
  type: 'System',
  isRead: false,
  priority: 'Normal',
  isExpired: false,
  createdAt: '2026-01-15T10:30:00Z',
  createdDate: '2026-01-15T10:30:00Z',
  actionUrl: null,
  referenceId: null,
  expiresAt: null,
  readAt: null,
  readDate: null,
  ...overrides,
});

describe('notification priority presentation', () => {
  it('maps the four known priorities case-insensitively', () => {
    expect(normalizePriority('Informational')).toBe('Informational');
    expect(normalizePriority('normal')).toBe('Normal');
    expect(normalizePriority('  IMPORTANT ')).toBe('Important');
    expect(normalizePriority('Critical')).toBe('Critical');
  });

  it('falls back to Normal for unknown or missing values', () => {
    // A legacy/garbled value must never break the notification centre.
    expect(normalizePriority(undefined)).toBe('Normal');
    expect(normalizePriority(null)).toBe('Normal');
    expect(normalizePriority('Urgent')).toBe('Normal');
  });

  it('orders severities from Informational to Critical', () => {
    expect(priorityRank('Informational')).toBeLessThan(priorityRank('Normal'));
    expect(priorityRank('Normal')).toBeLessThan(priorityRank('Important'));
    expect(priorityRank('Important')).toBeLessThan(priorityRank('Critical'));
  });

  it('gives Critical and Important a distinct colour from Normal', () => {
    expect(getNotificationVisuals(make({ priority: 'Critical' })).color).toBe('error');
    expect(getNotificationVisuals(make({ priority: 'Important' })).color).toBe('warning');
    expect(getNotificationVisuals(make({ priority: 'Informational' })).color).toBe('info');
  });

  it('always supplies a text severity label so colour is never the only signal', () => {
    for (const priority of ['Informational', 'Normal', 'Important', 'Critical'] as const) {
      expect(getNotificationVisuals(make({ priority })).severityLabel).toBeTruthy();
    }
  });

  it('treats a Security notification as severe regardless of its priority label', () => {
    const visuals = getNotificationVisuals(make({ type: 'Security', priority: 'Normal' }));
    expect(visuals.color).toBe('error');
    expect(visuals.severityLabel).toBe('Security');
  });
});

describe('notification action URLs', () => {
  it('accepts an application-relative path', () => {
    expect(getNotificationActionUrl(make({ actionUrl: '/assignments/abc' }))).toBe('/assignments/abc');
    expect(getNotificationActionUrl(make({ actionUrl: '/accommodation' }))).toBe('/accommodation');
  });

  it('rejects anything that could leave the origin', () => {
    // These are the open-redirect / XSS shapes. The server sanitises them too, but
    // the client must not be the only line of defence missing.
    expect(getNotificationActionUrl(make({ actionUrl: 'https://evil.example' }))).toBeNull();
    expect(getNotificationActionUrl(make({ actionUrl: '//evil.example' }))).toBeNull();
    expect(getNotificationActionUrl(make({ actionUrl: '/javascript:alert(1)' }))).toBeNull();
    expect(getNotificationActionUrl(make({ actionUrl: '/\\evil.example' }))).toBeNull();
    expect(getNotificationActionUrl(make({ actionUrl: 'javascript:alert(1)' }))).toBeNull();
  });

  it('returns null when no action target is present', () => {
    expect(getNotificationActionUrl(make({ actionUrl: null }))).toBeNull();
    expect(getNotificationActionUrl(make({ actionUrl: undefined }))).toBeNull();
    expect(getNotificationActionUrl(make({ actionUrl: '' }))).toBeNull();
  });

  it('labels the call-to-action per notification type', () => {
    expect(getNotificationActionLabel(make({ type: 'LectureNotes' }))).toBe('Open Lecture Notes');
    expect(getNotificationActionLabel(make({ type: 'Assignment' }))).toBe('Open Assignment');
    expect(getNotificationActionLabel(make({ type: 'Accommodation' }))).toBe('View Accommodation');
    expect(getNotificationActionLabel(make({ type: 'Unit' }))).toBe('View Units');
    expect(getNotificationActionLabel(make({ type: 'OmsRequest' }))).toBe('View Request');
    expect(getNotificationActionLabel(make({ type: 'Course' }))).toBe('View Course');
  });
});

describe('notification timestamps', () => {
  it('formats a valid timestamp', () => {
    const formatted = formatNotificationDate('2026-01-15T10:30:00Z');
    expect(formatted).not.toBe('—');
    expect(formatted).toContain('2026');
  });

  it('renders an em dash instead of "Invalid Date" for unusable values', () => {
    // The list previously showed "Invalid Date" on every row because the API
    // returns createdAt while the component read createdDate.
    expect(formatNotificationDate(undefined)).toBe('—');
    expect(formatNotificationDate(null)).toBe('—');
    expect(formatNotificationDate('')).toBe('—');
    expect(formatNotificationDate('not-a-date')).toBe('—');
  });
});