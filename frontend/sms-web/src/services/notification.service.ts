import { api } from './api';

export interface Notification {
  id: string;
  title: string;
  message: string;
  type: string;
  isRead: boolean;
  referenceId?: string;
  referenceType?: string;
  createdDate: string;
  readDate?: string;
}

export interface UnreadCount {
  count: number;
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
  pageSize?: number;
  isRead?: boolean;
}

export const notificationService = {
  getNotifications: (params: GetNotificationsParams = {}) =>
    api.get('/notifications', { params }),

  getNotification: (id: string) =>
    api.get(`/notifications/${id}`),

  markAsRead: (id: string) =>
    api.post(`/notifications/${id}/read`),

  markAllAsRead: () =>
    api.post('/notifications/read-all'),

  getUnreadCount: () =>
    api.get('/notifications/unread-count'),

  deleteNotification: (id: string) =>
    api.delete(`/notifications/${id}`),
};