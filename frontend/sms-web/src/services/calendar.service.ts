import { api } from './api';

export interface CalendarEvent {
  id: string;
  title: string;
  description?: string;
  startDate: string;
  endDate: string;
  location?: string;
  eventType: string;
  isAllDay?: boolean;
  color?: string;
  createdBy?: string;
  createdAt?: string;
}

export type CreateCalendarEventRequest = Omit<CalendarEvent, 'id' | 'createdAt'>;

export interface UpdateCalendarEventRequest extends Partial<CreateCalendarEventRequest> {}

export interface PagedResponse<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface GetEventsParams {
  startDate?: string;
  endDate?: string;
  eventType?: string;
  page?: number;
  pageSize?: number;
  searchTerm?: string;
}

export const calendarService = {
  getEvents: (params?: GetEventsParams) =>
    api.get<CalendarEvent[]>('/calendar-events', { params }),

  getEvent: (id: string) =>
    api.get<CalendarEvent>(`/calendar-events/${id}`),

  createEvent: (data: CreateCalendarEventRequest) =>
    api.post<CalendarEvent>('/calendar-events', data),

  updateEvent: (id: string, data: UpdateCalendarEventRequest) =>
    api.put<CalendarEvent>(`/calendar-events/${id}`, data),

  deleteEvent: (id: string) =>
    api.delete(`/calendar-events/${id}`),

  getUpcomingEvents: (limit?: number) =>
    api.get<CalendarEvent[]>('/calendar-events/upcoming', { params: { limit } }),

  getEventsInRange: (startDate: string, endDate: string) =>
    api.get<CalendarEvent[]>('/calendar-events/range', { params: { startDate, endDate } }),

  searchEvents: (params: { searchTerm: string; page?: number; pageSize?: number }) =>
    api.get<PagedResponse<CalendarEvent>>('/calendar-events/search', { params }),
};

