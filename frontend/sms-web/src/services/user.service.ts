import { api } from './api';
import { User } from '../types/user.types';

export interface PagedResponse<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface GetUsersParams {
  page?: number;
  pageSize?: number;
  searchTerm?: string;
  role?: string;
  isActive?: boolean;
  isEmailVerified?: boolean;
  sortBy?: string;
  sortDescending?: boolean;
}

export interface CreateUserRequest {
  title?: string;
  firstName: string;
  middleName?: string;
  lastName: string;
  email: string;
  phoneNumber?: string;
  userName?: string;
  roles?: string[];
  password?: string;
}

export interface UpdateUserRequest {
  title?: string;
  firstName?: string;
  middleName?: string;
  lastName?: string;
  email?: string;
  phoneNumber?: string;
  isActive?: boolean;
}

export interface AssignRolesRequest {
  userId: string;
  roles: string[];
}

export interface UserDetail extends User {
  userName?: string;
  phoneNumber?: string;
  lockoutEnd?: string;
  accessFailedCount?: number;
}

export interface LoginHistory {
  id: string;
  userId: string;
  loginTime: string;
  logoutTime?: string;
  ipAddress?: string;
  userAgent?: string;
  device?: string;
  location?: string;
  isSuccessful: boolean;
  failureReason?: string;
}

export const userService = {
  getUsers: (params: GetUsersParams) =>
    api.get<PagedResponse<UserDetail>>('/users', { params }),

  getUser: (id: string) =>
    api.get<UserDetail>(`/users/${id}`),

  createUser: (data: CreateUserRequest) =>
    api.post<UserDetail>('/users', data),

  updateUser: (id: string, data: UpdateUserRequest) =>
    api.put<UserDetail>(`/users/${id}`, data),

  deleteUser: (id: string) =>
    api.delete(`/users/${id}`),

  getUserRoles: (id: string) =>
    api.get<string[]>(`/users/${id}/roles`),

  assignRoles: (data: AssignRolesRequest) =>
    api.post(`/users/${data.userId}/roles`, { roles: data.roles }),

  removeRoles: (userId: string, roles: string[]) =>
    api.delete(`/users/${userId}/roles`, { data: { roles } }),

  activateUser: (id: string) =>
    api.post(`/users/${id}/activate`),

  deactivateUser: (id: string) =>
    api.post(`/users/${id}/deactivate`),

  resetPassword: (userId: string, newPassword: string) =>
    api.post(`/users/${userId}/reset-password`, { newPassword }),

  getLoginHistory: (userId: string, params?: { page?: number; pageSize?: number }) =>
    api.get<PagedResponse<LoginHistory>>(`/users/${userId}/login-history`, { params }),

  getProfile: () =>
    api.get<UserDetail>('/auth/me'),

  updateProfile: (data: Partial<UserDetail>) =>
    api.put<UserDetail>('/auth/profile', data),
};

