export interface User {
  id: string;
  title?: string;
  firstName: string;
  middleName?: string;
  lastName: string;
  displayName?: string;
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
  title?: string;
  firstName: string;
  middleName?: string;
  lastName: string;
  displayName?: string;
  roles: string[];
  permissions: string[];
  tenantId: string;
  expiresIn: number;
}

export interface RegisterRequest {
  title?: string;
  firstName: string;
  middleName?: string;
  lastName: string;
  email: string;
  password: string;
  confirmPassword: string;
  phoneNumber: string;
  organization?: string;
  role?: string;
  /**
   * Course chosen during registration. Required for students; required for
   * lecturers whenever `unitIds` is supplied.
   */
  courseId?: string;
  /**
   * Units the registrant selected.
   *
   * Students: the server enrolls every active unit of the chosen course, so
   * this is optional and is only used to detect a client/backend mismatch.
   * Lecturers: required - the server assigns exactly these units.
   */
  unitIds?: string[];
  specialization?: string;
}

/**
 * A unit shown on the registration verification (review) step.
 * Mirrors RegistrationUnitDto in SMS.Application.
 */
export interface RegistrationUnit {
  id: string;
  code: string;
  name: string;
  credits: number;
  semester: number;
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
