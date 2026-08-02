import { z } from 'zod';

export const emailSchema = z.string()
  .email('Invalid email address')
  .min(1, 'Email is required');

export const passwordSchema = z.string()
  .min(8, 'Password must be at least 8 characters')
  .regex(/[A-Z]/, 'Password must contain at least one uppercase letter')
  .regex(/[a-z]/, 'Password must contain at least one lowercase letter')
  .regex(/[0-9]/, 'Password must contain at least one number')
  .regex(/[^a-zA-Z0-9]/, 'Password must contain at least one special character');

export const nameSchema = z.string()
  .min(1, 'Name is required')
  .max(100, 'Name cannot exceed 100 characters');

export const phoneSchema = z.string()
  .min(1, 'Phone number is required')
  .max(20, 'Phone number cannot exceed 20 characters');

export const studentSchema = z.object({
  firstName: nameSchema,
  lastName: nameSchema,
  email: emailSchema,
  phoneNumber: phoneSchema,
  dateOfBirth: z.date({
    required_error: 'Date of birth is required',
    invalid_type_error: 'Invalid date',
  }),
  gender: z.string().optional(),
  address: z.string().optional(),
  programmeId: z.string().uuid().optional(),
});

export const courseSchema = z.object({
  name: z.string().min(1, 'Course name is required').max(100),
  code: z.string().min(1, 'Course code is required').max(20)
    .regex(/^[A-Z0-9]+$/, 'Course code must contain only uppercase letters and numbers'),
  duration: z.number().min(1, 'Duration must be at least 1 month'),
  totalCredits: z.number().min(1, 'Total credits must be at least 1'),
  departmentId: z.string().uuid('Department is required'),
  description: z.string().optional(),
});

export const unitSchema = z.object({
  name: z.string().min(1, 'Unit name is required').max(100),
  code: z.string().min(1, 'Unit code is required').max(20)
    .regex(/^[A-Z0-9]+$/, 'Unit code must contain only uppercase letters and numbers'),
  credits: z.number().min(1, 'Credits must be at least 1').max(6),
  contactHours: z.number().min(1, 'Contact hours must be at least 1'),
  courseId: z.string().uuid('Course is required'),
  prerequisiteUnitId: z.string().uuid().optional(),
});

export const loginSchema = z.object({
  email: emailSchema,
  password: z.string().min(1, 'Password is required'),
  rememberMe: z.boolean().optional(),
});

export const registerSchema = z.object({
  firstName: nameSchema,
  lastName: nameSchema,
  email: emailSchema,
  password: passwordSchema,
  confirmPassword: z.string().min(1, 'Please confirm your password'),
  phoneNumber: phoneSchema,
  organization: z.string().optional(),
  role: z.enum(['Student', 'Lecturer', 'Receptionist']).default('Student'),
}).refine((data) => data.password === data.confirmPassword, {
  message: 'Passwords do not match',
  path: ['confirmPassword'],
});

export const changePasswordSchema = z.object({
  currentPassword: z.string().min(1, 'Current password is required'),
  newPassword: passwordSchema,
  confirmNewPassword: z.string().min(1, 'Please confirm your new password'),
}).refine((data) => data.newPassword === data.confirmNewPassword, {
  message: 'Passwords do not match',
  path: ['confirmNewPassword'],
});

export const forgotPasswordSchema = z.object({
  email: emailSchema,
});

export const resetPasswordSchema = z.object({
  email: emailSchema,
  token: z.string().min(1, 'Reset token is required'),
  newPassword: passwordSchema,
  confirmPassword: z.string().min(1, 'Please confirm your password'),
}).refine((data) => data.newPassword === data.confirmPassword, {
  message: 'Passwords do not match',
  path: ['confirmPassword'],
});