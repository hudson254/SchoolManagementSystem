export interface User {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber?: string;
  organization?: string;
  roles: string[];
  permissions: string[];
  tenantId: string;
  isActive: boolean;
  isEmailVerified: boolean;
  lastLoginDate?: string;
  createdAt: string;
}

export interface UserProfile extends User {
  profileImage?: string;
  bio?: string;
}

export interface LoginRequest {
  email: string;
  password: string;
  rememberMe?: boolean;
}

export interface LoginResponse {
  accessToken: string;
  refreshToken: string;
  userId: string;
  email: string;
  firstName: string;
  lastName: string;
  roles: string[];
  permissions: string[];
  tenantId: string;
  expiresIn: number;
}

export interface RegisterRequest {
  firstName: string;
  lastName: string;
  email: string;
  password: string;
  confirmPassword: string;
  phoneNumber: string;
  organization?: string;
  role?: string;
}

export interface RegisterResponse extends LoginResponse {}

export interface ChangePasswordRequest {
  currentPassword: string;
  newPassword: string;
  confirmNewPassword: string;
}

export interface ForgotPasswordRequest {
  email: string;
}

export interface ResetPasswordRequest {
  email: string;
  token: string;
  newPassword: string;
  confirmPassword: string;
}

export interface VerifyEmailRequest {
  userId: string;
  token: string;
}

export interface UserRole {
  roleId: string;
  roleName: string;
}