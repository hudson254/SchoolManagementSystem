import { api } from './api';

/**
 * Notification severity, mirroring NotificationPriorities on the server.
 */
export type NotificationPriority = 'Informational' | 'Normal' | 'Important' | 'Critical';

export interface Notification {
  id: string;
  title: string;
  message: string;
  type: string;
  isRead: boolean;
  referenceId?: string | null;

  /**
   * Application-relative navigation target, already sanitised server-side.
   * It is a HINT only: navigating here does not bypass the API authorization
   * applied by the page the user lands on.
   */
  actionUrl?: string | null;

  priority: NotificationPriority;
  expiresAt?: string | null;
  isExpired: boolean;

  /**
   * Both spellings are published by the API for the same instant:
   * `createdAt` is canonical, `createdDate` is the legacy alias.
   * `createdAt` is what the UI reads; `createdDate` remains for older callers.
   */
  createdAt: string;
  createdDate: string;
  readAt?: string | null;
  readDate?: string | null;
}

export interface UnreadCount {
  count: number;
  /** True when at least one unread notification is Important or Critical. */
  hasCritical: boolean;
}

export interface PagedResponse<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface GetNotificationsParams {
  page?: number;
  /** Clamped server-side to a maximum of 100. */
  pageSize?: number;
  isRead?: boolean;
}

/** Default page size for the notification history list. */
export const NOTIFICATIONS_PAGE_SIZE = 20;

/**
 * Reads the creation timestamp defensively.
 *
 * The list previously rendered `new Date(notification.createdDate)` while the API
 * returned `createdAt`, so EVERY row showed "Invalid Date". Accepting either field
 * means a partial rollout of either naming cannot blank the timestamps again.
 */
export function getNotificationTimestamp(notification: Notification): string {
  return notification.createdAt || notification.createdDate;
}

export const notificationService = {
  getNotifications: (params: GetNotificationsParams = {}) =>
    api.get<PagedResponse<Notification>>('/notifications', { params }),

  getNotification: (id: string) =>
    api.get<Notification>(`/notifications/${id}`),

  markAsRead: (id: string) =>
    api.post<void>(`/notifications/${id}/read`),

  markAllAsRead: () =>
    api.post<void>('/notifications/read-all'),

  getUnreadCount: () =>
    api.get<UnreadCount>('/notifications/unread-count'),

  deleteNotification: (id: string) =>
    api.delete<void>(`/notifications/${id}`),
};