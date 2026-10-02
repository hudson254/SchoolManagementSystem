import type { Notification, NotificationPriority } from '../services/notification.service';

/**
 * Shared presentation rules for notifications.
 *
 * These live in one module because the header panel and the full-page notification
 * centre previously each had their own `switch`, and they disagreed: the centre
 * matched on 'info'/'warning'/'error' while the API writes catalogue types like
 * 'Accommodation' or 'Security', so every notification rendered as the neutral
 * default and severity was invisible. Severity is now driven by the PRIORITY
 * field, which the server sets; type only refines the icon.
 */

export interface NotificationVisuals {
  /** MUI colour token for the icon container. */
  color: 'info' | 'success' | 'warning' | 'error' | 'primary';
  /**
   * Short text label for the severity. Severity is NEVER conveyed by colour alone:
   * this label is the accessible signal, and it is rendered as text next to the
   * icon so the distinction survives a colour-blind or greyscale reading.
   */
  severityLabel: string;
  /** Numeric severity, for sorting unread items most-severe-first. */
  rank: number;
}

/** Falls back to Normal for an unrecognised value rather than throwing. */
export function normalizePriority(value: unknown): NotificationPriority {
  const known: NotificationPriority[] = ['Informational', 'Normal', 'Important', 'Critical'];
  if (typeof value === 'string') {
    const match = known.find((p) => p.toLowerCase() === value.trim().toLowerCase());
    if (match) return match;
  }
  return 'Normal';
}

/** Severity rank, lowest to highest. Used for sorting. */
export function priorityRank(value: unknown): number {
  switch (normalizePriority(value)) {
    case 'Informational': return 0;
    case 'Normal': return 1;
    case 'Important': return 2;
    case 'Critical': return 3;
    default: return 1;
  }
}

/**
 * Icon/colour for a notification.
 *
 * Priority drives the visual treatment; the type only refines it, so an Important
 * Accommodation notification is amber while an Important Security one is red.
 */
export function getNotificationVisuals(notification: Notification): NotificationVisuals {
  const priority = normalizePriority(notification.priority);
  const rank = priorityRank(priority);

  // Security-type items read as severe regardless of their priority label.
  const isSecurity = notification.type === 'Security';

  let color: NotificationVisuals['color'];
  if (isSecurity || priority === 'Critical') {
    color = 'error';
  } else if (priority === 'Important') {
    color = 'warning';
  } else if (priority === 'Informational') {
    color = 'info';
  } else {
    color = 'primary';
  }

  return {
    color,
    severityLabel: isSecurity && priority !== 'Critical' ? 'Security' : priority,
    rank,
  };
}

/**
 * A safe in-app navigation target for a notification.
 *
 * The server already sanitises actionUrl to a root-relative path, but it is
 * re-checked here so a notification can never send a user to an external origin.
 * This is a navigation convenience ONLY: the destination page and its API calls
 * remain the authorization boundary.
 */
export function getNotificationActionUrl(notification: Notification): string | null {
  const target = notification.actionUrl;
  if (!target || typeof target !== 'string') return null;

  const value = target.trim();
  if (!value.startsWith('/')) return null;
  if (value.startsWith('//')) return null;
  if (/^\/[a-zA-Z][a-zA-Z0-9+.-]*:/.test(value)) return null; // e.g. "/javascript:..."
  if (value.includes('\\')) return null;

  return value;
}

/** Human label for the call-to-action, derived from the notification type. */
export function getNotificationActionLabel(notification: Notification): string {
  switch (notification.type) {
    case 'LectureNotes': return 'Open Lecture Notes';
    case 'Assignment':
    case 'AssignmentSubmission':
    case 'AssignmentIssue': return 'Open Assignment';
    case 'Accommodation': return 'View Accommodation';
    case 'Unit': return 'View Units';
    case 'Course':
    case 'Enrollment': return 'View Course';
    case 'PermissionRequest':
    case 'Request':
    case 'OmsRequest': return 'View Request';
    case 'Certificate': return 'View Certificate';
    case 'Security':
    case 'Registration':
    case 'AccountApproval': return 'View Account';
    case 'Grade': return 'View Results';
    case 'Announcement': return 'Read Announcement';
    default: return 'Open';
  }
}

/**
 * Formats a notification timestamp.
 * Returns an em dash for an unparseable value instead of "Invalid Date", which is
 * what the list rendered before the createdAt/createdDate mismatch was fixed.
 */
export function formatNotificationDate(value: string | null | undefined): string {
  if (!value) return '—';
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return '—';
  return date.toLocaleString();
}