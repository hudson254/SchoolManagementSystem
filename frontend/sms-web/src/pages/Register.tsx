import React, { useState, useEffect, useCallback } from "react";
import { useNavigate, Link } from "react-router-dom";
import {
  Container,
  Box,
  Typography,
  TextField,
  Button,
  Paper,
  Alert,
  InputAdornment,
  IconButton,
  Stepper,
  Step,
  StepLabel,
  Card,
  CardActionArea,
  CardContent,
  Autocomplete,
  LinearProgress,
  CircularProgress,
  FormHelperText,
  Skeleton,
} from "@mui/material";
import {
  Visibility,
  VisibilityOff,
  Email,
  Lock,
  Person,
  Phone,
  School,
  Work,
  Business,
  ArrowBack,
  CheckCircle,
  Warning,
} from "@mui/icons-material";
import { useForm, Controller } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { useAuth } from "../hooks/useAuth";
import { apiClient } from "../services/api";
import { passwordSchema, emailSchema, nameSchema, phoneSchema } from "../utils/validators";

// ─── Types ───────────────────────────────────────────────────────────────────

interface CourseOption {
  id: string;
  name: string;
  code: string;
  credits: number;
  duration: number;
  description?: string;
}

interface UsernameAvailability {
  isAvailable: boolean;
  suggestedUsername?: string;
  message?: string;
}

type RegistrationRole = "Student" | "Lecturer";

// ─── Zod schemas ─────────────────────────────────────────────────────────────

const studentRegistrationSchema = z
  .object({
    firstName: nameSchema,
    lastName: nameSchema,
    email: emailSchema,
    phoneNumber: phoneSchema,
    organization: nameSchema,
    password: passwordSchema,
    confirmPassword: z.string().min(1, "Please confirm your password"),
    username: z
      .string()
      .regex(/^[a-z0-9]+$/, "Username may only contain lowercase letters and numbers")
      .min(3, "Username must be at least 3 characters")
      .max(50, "Username must not exceed 50 characters"),
    courseId: z.string().min(1, "Please select a course"),
  })
  .refine((data) => data.password === data.confirmPassword, {
    message: "Passwords do not match",
    path: ["confirmPassword"],
  });

const lecturerRegistrationSchema = z
  .object({
    firstName: nameSchema,
    lastName: nameSchema,
    email: emailSchema,
    phoneNumber: phoneSchema,
    organization: nameSchema,
    specialization: z
      .string()
      .min(1, "Specialization is required")
      .max(200, "Specialization must not exceed 200 characters"),
    password: passwordSchema,
    confirmPassword: z.string().min(1, "Please confirm your password"),
    username: z
      .string()
      .regex(/^[a-z0-9]+$/, "Username may only contain lowercase letters and numbers")
      .min(3, "Username must be at least 3 characters")
      .max(50, "Username must not exceed 50 characters"),
    courseId: z.string().min(1, "Please select a course"),
  })
  .refine((data) => data.password === data.confirmPassword, {
    message: "Passwords do not match",
    path: ["confirmPassword"],
  });

type StudentFormData = z.infer<typeof studentRegistrationSchema>;
type LecturerFormData = z.infer<typeof lecturerRegistrationSchema>;

// ─── Constants ───────────────────────────────────────────────────────────────

const STUDENT_STEPS = [
  "Personal Details",
  "Contact Details",
  "Account Details",
  "Course Selection",
  "Review & Submit",
];

const LECTURER_STEPS = [
  "Personal Details",
  "Contact Details",
  "Professional Info",
  "Account Details",
  "Course Assignment",
  "Review & Submit",
];

const ORGANIZATION_OPTIONS = [
  "Kenya Wildlife Service",
  "Kenya Forest Service",
  "Ministry of Tourism",
  "Private Institution",
  "Other",
];

const SPECIALIZATION_SUGGESTIONS = [
  "Wildlife Conservation",
  "GIS",
  "Cybersecurity",
  "Finance",
  "Leadership",
  "Environmental Law",
  "Education",
  "Healthcare",
];

const PRIMARY_COLOR = "#576426";

// ─── Password strength meter ────────────────────────────────────────────────

function getPasswordStrength(password: string): { score: number; label: string; color: string } {
  let score = 0;
  if (password.length >= 12) score += 25;
  if (password.length >= 16) score += 10;
  if (/[A-Z]/.test(password)) score += 15;
  if (/[a-z]/.test(password)) score += 15;
  if (/[0-9]/.test(password)) score += 15;
  if (/[^a-zA-Z0-9]/.test(password)) score += 20;

  if (score < 30) return { score, label: "Weak", color: "#f44336" };
  if (score < 60) return { score, label: "Fair", color: "#ff9800" };
  if (score < 80) return { score, label: "Good", color: "#2196f3" };
  return { score, label: "Strong", color: "#4caf50" };
}

// ─── Username generator ─────────────────────────────────────────────────────

function generateCandidateUsernames(firstName: string, lastName: string): string[] {
  const f = firstName.toLowerCase().replace(/[^a-z0-9]/g, "");
  const l = lastName.toLowerCase().replace(/[^a-z0-9]/g, "");
  if (!f || !l) return [];
  const candidates: string[] = [
    `${f}.${l}`,
    `${f}${l}`,
    `${f[0]}${l}`,
    `${f}${l[0]}`,
  ];
  return [...new Set(candidates)];
}

// ─── Component ──────────────────────────────────────────────────────────────

type RegistrationStep = "role-selection" | "student" | "lecturer" | "success";

export const Register: React.FC = () => {
  const navigate = useNavigate();
  const { register: registerUser } = useAuth();

  // Registration flow state
  const [step, setStep] = useState<RegistrationStep>("role-selection");
  const [role, setRole] = useState<RegistrationRole>("Student");

  // Wizard stepper state
  const [activeStep, setActiveStep] = useState(0);

  // UI state
  const [showPassword, setShowPassword] = useState(false);
  const [showConfirmPassword, setShowConfirmPassword] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);

  // Data state
  const [courses, setCourses] = useState<CourseOption[]>([]);
  const [coursesLoading, setCoursesLoading] = useState(false);
  const [usernameChecking, setUsernameChecking] = useState(false);
  const [usernameAvailable, setUsernameAvailable] = useState<boolean | null>(null);
  const [usernameMessage, setUsernameMessage] = useState<string | null>(null);
  const [candidateUsernames, setCandidateUsernames] = useState<string[]>([]);
  const [passwordStrength, setPasswordStrength] = useState(getPasswordStrength(""));

  // ─── Student form ────────────────────────────────────────────────────────
  const studentForm = useForm<StudentFormData>({
    resolver: zodResolver(studentRegistrationSchema),
    defaultValues: {
      firstName: "",
      lastName: "",
      email: "",
      phoneNumber: "",
      organization: "",
      password: "",
      confirmPassword: "",
      username: "",
      courseId: "",
    },
    mode: "onChange",
  });

  const studentValues = studentForm.watch();

  // ─── Lecturer form ────────────────────────────────────────────────────────
  const lecturerForm = useForm<LecturerFormData>({
    resolver: zodResolver(lecturerRegistrationSchema),
    defaultValues: {
      firstName: "",
      lastName: "",
      email: "",
      phoneNumber: "",
      organization: "",
      specialization: "",
      password: "",
      confirmPassword: "",
      username: "",
      courseId: "",
    },
    mode: "onChange",
  });

  const lecturerValues = lecturerForm.watch();

  // ─── Load courses ─────────────────────────────────────────────────────────
  useEffect(() => {
    const loadCourses = async () => {
      setCoursesLoading(true);
      try {
        const data = await apiClient.get<CourseOption[]>("/auth/active-courses");
        setCourses(data || []);
      } catch {
        setCourses([]);
      } finally {
        setCoursesLoading(false);
      }
    };
    loadCourses();
  }, []);

  // ─── Username generation & checking ──────────────────────────────────────
  const checkUsername = useCallback(async (username: string) => {
    if (!username || username.length < 3) {
      setUsernameAvailable(null);
      setUsernameMessage(null);
      return;
    }
    setUsernameChecking(true);
    try {
      const data = await apiClient.get<UsernameAvailability>(
        `/auth/username-availability?username=${encodeURIComponent(username)}`
      );
      setUsernameAvailable(data.isAvailable);
      setUsernameMessage(
        data.isAvailable
          ? "Username is available"
          : data.message || "Username is taken"
      );
    } catch {
      setUsernameAvailable(null);
      setUsernameMessage("Could not verify username availability");
    } finally {
      setUsernameChecking(false);
    }
  }, []);

  // Generate candidate usernames when first/last name changes
  useEffect(() => {
    const firstName =
      role === "Student" ? studentForm.watch("firstName") : lecturerForm.watch("firstName");
    const lastName =
      role === "Student" ? studentForm.watch("lastName") : lecturerForm.watch("lastName");

    if (firstName && lastName) {
      setCandidateUsernames(generateCandidateUsernames(firstName, lastName));
    } else {
      setCandidateUsernames([]);
    }
  }, [
    role,
    studentForm.watch("firstName"),
    studentForm.watch("lastName"),
    lecturerForm.watch("firstName"),
    lecturerForm.watch("lastName"),
  ]);

  // Debounced username availability check
  useEffect(() => {
    const username =
      role === "Student" ? studentForm.watch("username") : lecturerForm.watch("username");
    const timer = setTimeout(() => {
      if (username) checkUsername(username);
    }, 500);
    return () => clearTimeout(timer);
  }, [
    role,
    studentForm.watch("username"),
    lecturerForm.watch("username"),
    checkUsername,
  ]);

  // Password strength tracking
  useEffect(() => {
    const pwd =
      role === "Student" ? studentForm.watch("password") : lecturerForm.watch("password");
    setPasswordStrength(getPasswordStrength(pwd || ""));
  }, [
    role,
    studentForm.watch("password"),
    lecturerForm.watch("password"),
  ]);

  // ─── Role Selection ──────────────────────────────────────────────────────
  const handleRoleSelect = (selectedRole: RegistrationRole) => {
    setRole(selectedRole);
    setError(null);
    setActiveStep(0);
    setStep(selectedRole === "Student" ? "student" : "lecturer");
  };

  // ─── Navigation ──────────────────────────────────────────────────────────
  const handleNext = () => {
    const steps = role === "Student" ? STUDENT_STEPS : LECTURER_STEPS;
    setActiveStep((prev) => Math.min(prev + 1, steps.length - 1));
  };

  const handleBack = () => {
    if (activeStep === 0) {
      setStep("role-selection");
      setError(null);
    } else {
      setActiveStep((prev) => Math.max(prev - 1, 0));
    }
  };

  const goToStep = (stepIndex: number) => {
    setActiveStep(stepIndex);
  };

  // ─── Submission ──────────────────────────────────────────────────────────
  const handleStudentSubmit = async (data: StudentFormData) => {
    setError(null);
    setLoading(true);
    try {
      await registerUser({
        firstName: data.firstName,
        lastName: data.lastName,
        email: data.email,
        password: data.password,
        confirmPassword: data.confirmPassword,
        phoneNumber: data.phoneNumber,
        organization: data.organization,
        role: "Student",
        username: data.username,
        courseId: data.courseId,
      });
      setSuccessMessage(
        "Registration successful! You have been enrolled in your selected course and its active units."
      );
      setStep("success");
    } catch (err: any) {
      setError(
        err.response?.data?.message || err.response?.data?.title || "Registration failed. Please try again."
      );
    } finally {
      setLoading(false);
    }
  };

  const handleLecturerSubmit = async (data: LecturerFormData) => {
    setError(null);
    setLoading(true);
    try {
      await registerUser({
        firstName: data.firstName,
        lastName: data.lastName,
        email: data.email,
        password: data.password,
        confirmPassword: data.confirmPassword,
        phoneNumber: data.phoneNumber,
        organization: data.organization,
        role: "Lecturer",
        username: data.username,
        courseId: data.courseId,
        specialization: data.specialization,
      });
      setSuccessMessage(
        "Registration successful! You have been assigned to teach the selected course and its active units."
      );
      setStep("success");
    } catch (err: any) {
      setError(
        err.response?.data?.message || err.response?.data?.title || "Registration failed. Please try again."
      );
    } finally {
      setLoading(false);
    }
  };

  // ─── Validation Helpers for Step Navigation ──────────────────────────────
  const getStudentFieldsForStep = (stepIndex: number): (keyof StudentFormData)[] => {
    switch (stepIndex) {
      case 0: return ["firstName", "lastName", "organization"];
      case 1: return ["email", "phoneNumber"];
      case 2: return ["password", "confirmPassword", "username"];
      case 3: return ["courseId"];
      default: return [];
    }
  };

  const getLecturerFieldsForStep = (stepIndex: number): (keyof LecturerFormData)[] => {
    switch (stepIndex) {
      case 0: return ["firstName", "lastName", "organization"];
      case 1: return ["email", "phoneNumber"];
      case 2: return ["specialization"];
      case 3: return ["password", "confirmPassword", "username"];
      case 4: return ["courseId"];
      default: return [];
    }
  };

  const canProceed = (): boolean => {
    const fields =
      role === "Student"
        ? getStudentFieldsForStep(activeStep)
        : getLecturerFieldsForStep(activeStep);
    const form = role === "Student" ? studentForm : lecturerForm;
    const errors = form.formState.errors;
    const values = form.getValues();
    return fields.every((field) => {
      const value = values[field as keyof typeof values];
      return value !== undefined && value !== "" && !errors[field as keyof typeof errors];
    });
  };

  // ─── Render: Role Selection ──────────────────────────────────────────────
  const renderRoleSelection = () => (
    <Paper elevation={3} sx={{ p: { xs: 3, sm: 5 }, width: "100%", borderRadius: 2 }}>
      <Box sx={{ textAlign: "center", mb: 5 }}>
        <Box
          component="img"
          src="/logo.png"
          alt="School Logo"
          sx={{
            width: 90,
            height: 90,
            mb: 2,
            borderRadius: 2,
            objectFit: "contain",
          }}
          onError={(e: any) => {
            e.target.style.display = "none";
          }}
        />
        <Typography
          variant="h4"
          sx={{ color: PRIMARY_COLOR, fontWeight: 700, mb: 1 }}
        >
          Create Account
        </Typography>
        <Typography variant="body1" color="text.secondary" sx={{ mb: 0.5 }}>
          Join the School Management System
        </Typography>
        <Typography variant="body2" color="text.secondary">
          Select your registration type to get started
        </Typography>
      </Box>

      <Box
        sx={{
          display: "flex",
          flexDirection: { xs: "column", md: "row" },
          gap: 3,
          mb: 4,
        }}
      >
        <Card
          sx={{
            flex: 1,
            cursor: "pointer",
            transition: "transform 0.2s, box-shadow 0.2s",
            "&:hover": {
              transform: "translateY(-4px)",
              boxShadow: 6,
              borderColor: PRIMARY_COLOR,
            },
            "&:focus-within": {
              outline: `3px solid ${PRIMARY_COLOR}`,
              outlineOffset: 2,
            },
            border: "2px solid transparent",
          }}
          role="button"
          tabIndex={0}
          aria-label="Register as Student"
          onClick={() => handleRoleSelect("Student")}
          onKeyDown={(e) => {
            if (e.key === "Enter" || e.key === " ") {
              e.preventDefault();
              handleRoleSelect("Student");
            }
          }}
        >
          <CardActionArea sx={{ p: 2 }}>
            <CardContent sx={{ textAlign: "center" }}>
              <School sx={{ fontSize: 64, color: PRIMARY_COLOR, mb: 2 }} />
              <Typography variant="h5" fontWeight={600} gutterBottom>
                Register as Student
              </Typography>
              <Typography variant="body2" color="text.secondary">
                Enroll in courses, access learning materials, track your
                academic progress, and manage your studies.
              </Typography>
            </CardContent>
          </CardActionArea>
        </Card>

        <Card
          sx={{
            flex: 1,
            cursor: "pointer",
            transition: "transform 0.2s, box-shadow 0.2s",
            "&:hover": {
              transform: "translateY(-4px)",
              boxShadow: 6,
              borderColor: PRIMARY_COLOR,
            },
            "&:focus-within": {
              outline: `3px solid ${PRIMARY_COLOR}`,
              outlineOffset: 2,
            },
            border: "2px solid transparent",
          }}
          role="button"
          tabIndex={0}
          aria-label="Register as Lecturer"
          onClick={() => handleRoleSelect("Lecturer")}
          onKeyDown={(e) => {
            if (e.key === "Enter" || e.key === " ") {
              e.preventDefault();
              handleRoleSelect("Lecturer");
            }
          }}
        >
          <CardActionArea sx={{ p: 2 }}>
            <CardContent sx={{ textAlign: "center" }}>
              <Work sx={{ fontSize: 64, color: PRIMARY_COLOR, mb: 2 }} />
              <Typography variant="h5" fontWeight={600} gutterBottom>
                Register as Lecturer
              </Typography>
              <Typography variant="body2" color="text.secondary">
                Teach courses, manage assignments, grade students, and track
                your teaching schedule.
              </Typography>
            </CardContent>
          </CardActionArea>
        </Card>
      </Box>

      <Box sx={{ textAlign: "center" }}>
        <Typography variant="body2" color="text.secondary">
          Already have an account?{" "}
          <Link
            to="/login"
            style={{ color: PRIMARY_COLOR, fontWeight: 600 }}
          >
            Sign in
          </Link>
        </Typography>
      </Box>
    </Paper>
  );

  // ─── Render: Student Step Content ────────────────────────────────────────
  const renderStudentStep = (stepIndex: number) => {
    const { control, formState: { errors } } = studentForm;

    switch (stepIndex) {
      case 0:
        return (
          <Box>
            <Typography variant="subtitle1" fontWeight={600} gutterBottom>
              Personal Details
            </Typography>
            <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
              Please provide your personal information
            </Typography>
            <Controller
              name="firstName"
              control={control}
              render={({ field }) => (
                <TextField
                  {...field}
                  fullWidth
                  label="First Name *"
                  variant="outlined"
                  margin="normal"
                  error={!!errors.firstName}
                  helperText={errors.firstName?.message}
                  InputProps={{
                    startAdornment: (
                      <InputAdornment position="start">
                        <Person color="action" />
                      </InputAdornment>
                    ),
                  }}
                />
              )}
            />
            <Controller
              name="lastName"
              control={control}
              render={({ field }) => (
                <TextField
                  {...field}
                  fullWidth
                  label="Last Name *"
                  variant="outlined"
                  margin="normal"
                  error={!!errors.lastName}
                  helperText={errors.lastName?.message}
                  InputProps={{
                    startAdornment: (
                      <InputAdornment position="start">
                        <Person color="action" />
                      </InputAdornment>
                    ),
                  }}
                />
              )}
            />
            <Controller
              name="organization"
              control={control}
              render={({ field }) => (
                <Autocomplete
                  freeSolo
                  options={ORGANIZATION_OPTIONS}
                  value={field.value}
                  onChange={(_, newValue) => field.onChange(newValue || "")}
                  onInputChange={(_, newValue) => field.onChange(newValue)}
                  renderInput={(params) => (
                    <TextField
                      {...params}
                      fullWidth
                      label="Organization / Institution *"
                      variant="outlined"
                      margin="normal"
                      error={!!errors.organization}
                      helperText={errors.organization?.message}
                      InputProps={{
                        ...params.InputProps,
                        startAdornment: (
                          <InputAdornment position="start">
                            <Business color="action" />
                          </InputAdornment>
                        ),
                      }}
                    />
                  )}
                />
              )}
            />
          </Box>
        );

      case 1:
        return (
          <Box>
            <Typography variant="subtitle1" fontWeight={600} gutterBottom>
              Contact Details
            </Typography>
            <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
              How can we reach you?
            </Typography>
            <Controller
              name="email"
              control={control}
              render={({ field }) => (
                <TextField
                  {...field}
                  fullWidth
                  label="Email Address *"
                  variant="outlined"
                  margin="normal"
                  error={!!errors.email}
                  helperText={errors.email?.message}
                  InputProps={{
                    startAdornment: (
                      <InputAdornment position="start">
                        <Email color="action" />
                      </InputAdornment>
                    ),
                  }}
                />
              )}
            />
            <Controller
              name="phoneNumber"
              control={control}
              render={({ field }) => (
                <TextField
                  {...field}
                  fullWidth
                  label="Phone Number *"
                  variant="outlined"
                  margin="normal"
                  error={!!errors.phoneNumber}
                  helperText={errors.phoneNumber?.message}
                  InputProps={{
                    startAdornment: (
                      <InputAdornment position="start">
                        <Phone color="action" />
                      </InputAdornment>
                    ),
                  }}
                />
              )}
            />
          </Box>
        );

      case 2:
        return (
          <Box>
            <Typography variant="subtitle1" fontWeight={600} gutterBottom>
              Account Details
            </Typography>
            <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
              Create your account credentials
            </Typography>

            <Box sx={{ mb: 1 }}>
              <Typography variant="body2" color="text.secondary" gutterBottom>
                Username
              </Typography>
              {candidateUsernames.length > 0 && (
                <Box sx={{ mb: 1, display: "flex", gap: 0.5, flexWrap: "wrap" }}>
                  {candidateUsernames.slice(0, 4).map((candidate) => (
                    <Button
                      key={candidate}
                      size="small"
                      variant="outlined"
                      sx={{
                        fontSize: "0.75rem",
                        borderColor: "#ccc",
                        color: "text.secondary",
                        "&:hover": { borderColor: PRIMARY_COLOR, color: PRIMARY_COLOR },
                      }}
                      onClick={() => {
                        studentForm.setValue("username", candidate);
                        checkUsername(candidate);
                      }}
                    >
                      {candidate}
                    </Button>
                  ))}
                </Box>
              )}
              <Controller
                name="username"
                control={control}
                render={({ field }) => (
                  <TextField
                    {...field}
                    fullWidth
                    label="Username *"
                    variant="outlined"
                    margin="dense"
                    error={!!errors.username || (usernameAvailable === false && !!field.value)}
                    helperText={
                      errors.username?.message ||
                      (usernameChecking
                        ? "Checking availability..."
                        : usernameAvailable === true
                          ? "Username is available"
                          : usernameAvailable === false
                            ? usernameMessage || "Username is taken"
                            : "")
                    }
                    FormHelperTextProps={{
                      sx: {
                        color:
                          usernameAvailable === true
                            ? "success.main"
                            : usernameAvailable === false
                              ? "error.main"
                              : undefined,
                      },
                    }}
                    InputProps={{
                      startAdornment: (
                        <InputAdornment position="start">
                          <Person color="action" />
                        </InputAdornment>
                      ),
                      endAdornment: usernameChecking ? (
                        <InputAdornment position="end">
                          <CircularProgress size={20} />
                        </InputAdornment>
                      ) : usernameAvailable === true ? (
                        <InputAdornment position="end">
                          <CheckCircle sx={{ color: "success.main", fontSize: 20 }} />
                        </InputAdornment>
                      ) : usernameAvailable === false ? (
                        <InputAdornment position="end">
                          <Warning sx={{ color: "error.main", fontSize: 20 }} />
                        </InputAdornment>
                      ) : null,
                    }}
                  />
                )}
              />
            </Box>

            <Controller
              name="password"
              control={control}
              render={({ field }) => (
                <TextField
                  {...field}
                  fullWidth
                  label="Password *"
                  variant="outlined"
                  margin="normal"
                  type={showPassword ? "text" : "password"}
                  error={!!errors.password}
                  helperText={errors.password?.message}
                  InputProps={{
                    startAdornment: (
                      <InputAdornment position="start">
                        <Lock color="action" />
                      </InputAdornment>
                    ),
                    endAdornment: (
                      <InputAdornment position="end">
                        <IconButton
                          onClick={() => setShowPassword(!showPassword)}
                          edge="end"
                          aria-label="toggle password visibility"
                        >
                          {showPassword ? <VisibilityOff /> : <Visibility />}
                        </IconButton>
                      </InputAdornment>
                    ),
                  }}
                />
              )}
            />
            {studentValues.password && (
              <Box sx={{ mt: 1, mb: 1 }}>
                <LinearProgress
                  variant="determinate"
                  value={Math.min(passwordStrength.score, 100)}
                  sx={{
                    height: 6,
                    borderRadius: 3,
                    backgroundColor: "#e0e0e0",
                    "& .MuiLinearProgress-bar": {
                      backgroundColor: passwordStrength.color,
                    },
                  }}
                />
                <FormHelperText sx={{ color: passwordStrength.color }}>
                  Password strength: {passwordStrength.label}
                </FormHelperText>
              </Box>
            )}

            <Controller
              name="confirmPassword"
              control={control}
              render={({ field }) => (
                <TextField
                  {...field}
                  fullWidth
                  label="Confirm Password *"
                  variant="outlined"
                  margin="normal"
                  type={showConfirmPassword ? "text" : "password"}
                  error={!!errors.confirmPassword}
                  helperText={errors.confirmPassword?.message}
                  InputProps={{
                    startAdornment: (
                      <InputAdornment position="start">
                        <Lock color="action" />
                      </InputAdornment>
                    ),
                    endAdornment: (
                      <InputAdornment position="end">
                        <IconButton
                          onClick={() => setShowConfirmPassword(!showConfirmPassword)}
                          edge="end"
                          aria-label="toggle confirm password visibility"
                        >
                          {showConfirmPassword ? <VisibilityOff /> : <Visibility />}
                        </IconButton>
                      </InputAdornment>
                    ),
                  }}
                />
              )}
            />
          </Box>
        );

      case 3:
        return (
          <Box>
            <Typography variant="subtitle1" fontWeight={600} gutterBottom>
              Course Selection
            </Typography>
            <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
              Select the course you wish to enroll in
            </Typography>
            {coursesLoading ? (
              <Skeleton variant="rectangular" height={56} sx={{ borderRadius: 1 }} />
            ) : (
              <Controller
                name="courseId"
                control={control}
                render={({ field }) => (
                  <Autocomplete
                    options={courses}
                    loading={coursesLoading}
                    getOptionLabel={(option) =>
                      typeof option === "string"
                        ? option
                        : `${option.name} (${option.code})`
                    }
                    value={courses.find((c) => c.id === field.value) || null}
                    onChange={(_, newValue) => {
                      field.onChange(newValue?.id || "");
                    }}
                    isOptionEqualToValue={(option, value) => option.id === value.id}
                    renderInput={(params) => (
                      <TextField
                        {...params}
                        fullWidth
                        label="Search for a course *"
                        variant="outlined"
                        margin="normal"
                        error={!!errors.courseId}
                        helperText={errors.courseId?.message}
                        InputProps={{
                          ...params.InputProps,
                          endAdornment: (
                            <>
                              {coursesLoading ? (
                                <CircularProgress color="inherit" size={20} />
                              ) : null}
                              {params.InputProps.endAdornment}
                            </>
                          ),
                        }}
                      />
                    )}
                    renderOption={(props, option) => (
                      <li {...props} key={option.id}>
                        <Box>
                          <Typography variant="body2" fontWeight={500}>
                            {option.name}
                          </Typography>
                          <Typography variant="caption" color="text.secondary">
                            {option.code} &middot; {option.duration} months &middot;{" "}
                            {option.credits} credits
                          </Typography>
                        </Box>
                      </li>
                    )}
                    noOptionsText={
                      courses.length === 0 && !coursesLoading
                        ? "No courses available for registration"
                        : "No matching courses"
                    }
                  />
                )}
              />
            )}
          </Box>
        );

      case 4:
        return (
          <Box>
            <Typography variant="subtitle1" fontWeight={600} gutterBottom>
              Review Your Registration
            </Typography>
            <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
              Please verify your information before submitting
            </Typography>
            <Box
              sx={{
                bgcolor: "grey.50",
                p: 3,
                borderRadius: 1,
                border: "1px solid",
                borderColor: "grey.200",
              }}
            >
              <Box sx={{ mb: 2 }}>
                <Typography variant="subtitle2" color={PRIMARY_COLOR} gutterBottom>
                  Personal Details
                </Typography>
                <Typography variant="body2">
                  {studentValues.firstName} {studentValues.lastName}
                </Typography>
                <Typography variant="body2" color="text.secondary">
                  {studentValues.organization}
                </Typography>
              </Box>
              <Box sx={{ mb: 2 }}>
                <Typography variant="subtitle2" color={PRIMARY_COLOR} gutterBottom>
                  Contact Details
                </Typography>
                <Typography variant="body2">{studentValues.email}</Typography>
                <Typography variant="body2" color="text.secondary">
                  {studentValues.phoneNumber}
                </Typography>
              </Box>
              <Box sx={{ mb: 2 }}>
                <Typography variant="subtitle2" color={PRIMARY_COLOR} gutterBottom>
                  Account Details
                </Typography>
                <Typography variant="body2">
                  Username: {studentValues.username}
                </Typography>
              </Box>
              <Box>
                <Typography variant="subtitle2" color={PRIMARY_COLOR} gutterBottom>
                  Course Selection
                </Typography>
                <Typography variant="body2" fontWeight={500}>
                  {courses.find((c) => c.id === studentValues.courseId)?.name ||
                    "Selected course"}
                </Typography>
                {(() => {
                  const course = courses.find((c) => c.id === studentValues.courseId);
                  return course ? (
                    <Typography variant="body2" color="text.secondary">
                      {course.code} &middot; {course.duration} months &middot;{" "}
                      {course.credits} credits
                    </Typography>
                  ) : null;
                })()}
              </Box>
            </Box>
          </Box>
        );

      default:
        return null;
    }
  };

  // ─── Render: Lecturer Step Content ───────────────────────────────────────
  const renderLecturerStep = (stepIndex: number) => {
    const { control, formState: { errors } } = lecturerForm;

    switch (stepIndex) {
      case 0:
        return (
          <Box>
            <Typography variant="subtitle1" fontWeight={600} gutterBottom>
              Personal Details
            </Typography>
            <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
              Please provide your personal information
            </Typography>
            <Controller
              name="firstName"
              control={control}
              render={({ field }) => (
                <TextField
                  {...field}
                  fullWidth
                  label="First Name *"
                  variant="outlined"
                  margin="normal"
                  error={!!errors.firstName}
                  helperText={errors.firstName?.message}
                  InputProps={{
                    startAdornment: (
                      <InputAdornment position="start">
                        <Person color="action" />
                      </InputAdornment>
                    ),
                  }}
                />
              )}
            />
            <Controller
              name="lastName"
              control={control}
              render={({ field }) => (
                <TextField
                  {...field}
                  fullWidth
                  label="Last Name *"
                  variant="outlined"
                  margin="normal"
                  error={!!errors.lastName}
                  helperText={errors.lastName?.message}
                  InputProps={{
                    startAdornment: (
                      <InputAdornment position="start">
                        <Person color="action" />
                      </InputAdornment>
                    ),
                  }}
                />
              )}
            />
            <Controller
              name="organization"
              control={control}
              render={({ field }) => (
                <Autocomplete
                  freeSolo
                  options={ORGANIZATION_OPTIONS}
                  value={field.value}
                  onChange={(_, newValue) => field.onChange(newValue || "")}
                  onInputChange={(_, newValue) => field.onChange(newValue)}
                  renderInput={(params) => (
                    <TextField
                      {...params}
                      fullWidth
                      label="Organization / Institution *"
                      variant="outlined"
                      margin="normal"
                      error={!!errors.organization}
                      helperText={errors.organization?.message}
                      InputProps={{
                        ...params.InputProps,
                        startAdornment: (
                          <InputAdornment position="start">
                            <Business color="action" />
                          </InputAdornment>
                        ),
                      }}
                    />
                  )}
                />
              )}
            />
          </Box>
        );

      case 1:
        return (
          <Box>
            <Typography variant="subtitle1" fontWeight={600} gutterBottom>
              Contact Details
            </Typography>
            <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
              How can we reach you?
            </Typography>
            <Controller
              name="email"
              control={control}
              render={({ field }) => (
                <TextField
                  {...field}
                  fullWidth
                  label="Email Address *"
                  variant="outlined"
                  margin="normal"
                  autoComplete="email"
                  error={!!errors.email}
                  helperText={errors.email?.message}
                  InputProps={{
                    startAdornment: (
                      <InputAdornment position="start">
                        <Email color="action" />
                      </InputAdornment>
                    ),
                  }}
                />
              )}
            />
            <Controller
              name="phoneNumber"
              control={control}
              render={({ field }) => (
                <TextField
                  {...field}
                  fullWidth
                  label="Phone Number *"
                  variant="outlined"
                  margin="normal"
                  error={!!errors.phoneNumber}
                  helperText={errors.phoneNumber?.message}
                  InputProps={{
                    startAdornment: (
                      <InputAdornment position="start">
                        <Phone color="action" />
                      </InputAdornment>
                    ),
                  }}
                />
              )}
            />
          </Box>
        );

      case 2:
        return (
          <Box>
            <Typography variant="subtitle1" fontWeight={600} gutterBottom>
              Professional Information
            </Typography>
            <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
              Tell us about your area of expertise
            </Typography>
            <Controller
              name="specialization"
              control={control}
              render={({ field }) => (
                <Autocomplete
                  freeSolo
                  options={SPECIALIZATION_SUGGESTIONS}
                  value={field.value}
                  onChange={(_, newValue) => field.onChange(newValue || "")}
                  onInputChange={(_, newValue) => field.onChange(newValue)}
                  renderInput={(params) => (
                    <TextField
                      {...params}
                      fullWidth
                      label="Specialization / Area of Expertise *"
                      variant="outlined"
                      margin="normal"
                      error={!!errors.specialization}
                      helperText={errors.specialization?.message || "e.g., Wildlife Conservation, GIS, Cybersecurity"}
                      InputProps={{
                        ...params.InputProps,
                        startAdornment: (
                          <InputAdornment position="start">
                            <Work color="action" />
                          </InputAdornment>
                        ),
                      }}
                    />
                  )}
                />
              )}
            />
          </Box>
        );

      case 3:
        return (
          <Box>
            <Typography variant="subtitle1" fontWeight={600} gutterBottom>
              Account Details
            </Typography>
            <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
              Create your account credentials
            </Typography>

            <Box sx={{ mb: 1 }}>
              <Typography variant="body2" color="text.secondary" gutterBottom>
                Username
              </Typography>
              {candidateUsernames.length > 0 && (
                <Box sx={{ mb: 1, display: "flex", gap: 0.5, flexWrap: "wrap" }}>
                  {candidateUsernames.slice(0, 4).map((candidate) => (
                    <Button
                      key={candidate}
                      size="small"
                      variant="outlined"
                      sx={{
                        fontSize: "0.75rem",
                        borderColor: "#ccc",
                        color: "text.secondary",
                        "&:hover": { borderColor: PRIMARY_COLOR, color: PRIMARY_COLOR },
                      }}
                      onClick={() => {
                        lecturerForm.setValue("username", candidate);
                        checkUsername(candidate);
                      }}
                    >
                      {candidate}
                    </Button>
                  ))}
                </Box>
              )}
              <Controller
                name="username"
                control={control}
                render={({ field }) => (
                  <TextField
                    {...field}
                    fullWidth
                    label="Username *"
                    variant="outlined"
                    margin="dense"
                    error={!!errors.username || (usernameAvailable === false && !!field.value)}
                    helperText={
                      errors.username?.message ||
                      (usernameChecking
                        ? "Checking availability..."
                        : usernameAvailable === true
                          ? "Username is available"
                          : usernameAvailable === false
                            ? usernameMessage || "Username is taken"
                            : "")
                    }
                    FormHelperTextProps={{
                      sx: {
                        color:
                          usernameAvailable === true
                            ? "success.main"
                            : usernameAvailable === false
                              ? "error.main"
                              : undefined,
                      },
                    }}
                    InputProps={{
                      startAdornment: (
                        <InputAdornment position="start">
                          <Person color="action" />
                        </InputAdornment>
                      ),
                      endAdornment: usernameChecking ? (
                        <InputAdornment position="end">
                          <CircularProgress size={20} />
                        </InputAdornment>
                      ) : usernameAvailable === true ? (
                        <InputAdornment position="end">
                          <CheckCircle sx={{ color: "success.main", fontSize: 20 }} />
                        </InputAdornment>
                      ) : usernameAvailable === false ? (
                        <InputAdornment position="end">
                          <Warning sx={{ color: "error.main", fontSize: 20 }} />
                        </InputAdornment>
                      ) : null,
                    }}
                  />
                )}
              />
            </Box>

            <Controller
              name="password"
              control={control}
              render={({ field }) => (
                <TextField
                  {...field}
                  fullWidth
                  label="Password *"
                  variant="outlined"
                  margin="normal"
                  type={showPassword ? "text" : "password"}
                  error={!!errors.password}
                  helperText={errors.password?.message}
                  InputProps={{
                    startAdornment: (
                      <InputAdornment position="start">
                        <Lock color="action" />
                      </InputAdornment>
                    ),
                    endAdornment: (
                      <InputAdornment position="end">
                        <IconButton
                          onClick={() => setShowPassword(!showPassword)}
                          edge="end"
                          aria-label="toggle password visibility"
                        >
                          {showPassword ? <VisibilityOff /> : <Visibility />}
                        </IconButton>
                      </InputAdornment>
                    ),
                  }}
                />
              )}
            />
            {lecturerValues.password && (
              <Box sx={{ mt: 1, mb: 1 }}>
                <LinearProgress
                  variant="determinate"
                  value={Math.min(passwordStrength.score, 100)}
                  sx={{
                    height: 6,
                    borderRadius: 3,
                    backgroundColor: "#e0e0e0",
                    "& .MuiLinearProgress-bar": {
                      backgroundColor: passwordStrength.color,
                    },
                  }}
                />
                <FormHelperText sx={{ color: passwordStrength.color }}>
                  Password strength: {passwordStrength.label}
                </FormHelperText>
              </Box>
            )}

            <Controller
              name="confirmPassword"
              control={control}
              render={({ field }) => (
                <TextField
                  {...field}
                  fullWidth
                  label="Confirm Password *"
                  variant="outlined"
                  margin="normal"
                  type={showConfirmPassword ? "text" : "password"}
                  error={!!errors.confirmPassword}
                  helperText={errors.confirmPassword?.message}
                  InputProps={{
                    startAdornment: (
                      <InputAdornment position="start">
                        <Lock color="action" />
                      </InputAdornment>
                    ),
                    endAdornment: (
                      <InputAdornment position="end">
                        <IconButton
                          onClick={() => setShowConfirmPassword(!showConfirmPassword)}
                          edge="end"
                          aria-label="toggle confirm password visibility"
                        >
                          {showConfirmPassword ? <VisibilityOff /> : <Visibility />}
                        </IconButton>
                      </InputAdornment>
                    ),
                  }}
                />
              )}
            />
          </Box>
        );

      case 4:
        return (
          <Box>
            <Typography variant="subtitle1" fontWeight={600} gutterBottom>
              Course Assignment
            </Typography>
            <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
              Select the course you will be teaching
            </Typography>
            {coursesLoading ? (
              <Skeleton variant="rectangular" height={56} sx={{ borderRadius: 1 }} />
            ) : (
              <Controller
                name="courseId"
                control={control}
                render={({ field }) => (
                  <Autocomplete
                    options={courses}
                    loading={coursesLoading}
                    getOptionLabel={(option) =>
                      typeof option === "string"
                        ? option
                        : `${option.name} (${option.code})`
                    }
                    value={courses.find((c) => c.id === field.value) || null}
                    onChange={(_, newValue) => {
                      field.onChange(newValue?.id || "");
                    }}
                    isOptionEqualToValue={(option, value) => option.id === value.id}
                    renderInput={(params) => (
                      <TextField
                        {...params}
                        fullWidth
                        label="Search for a course *"
                        variant="outlined"
                        margin="normal"
                        error={!!errors.courseId}
                        helperText={errors.courseId?.message}
                        InputProps={{
                          ...params.InputProps,
                          endAdornment: (
                            <>
                              {coursesLoading ? (
                                <CircularProgress color="inherit" size={20} />
                              ) : null}
                              {params.InputProps.endAdornment}
                            </>
                          ),
                        }}
                      />
                    )}
                    renderOption={(props, option) => (
                      <li {...props} key={option.id}>
                        <Box>
                          <Typography variant="body2" fontWeight={500}>
                            {option.name}
                          </Typography>
                          <Typography variant="caption" color="text.secondary">
                            {option.code} &middot; {option.duration} months &middot;{" "}
                            {option.credits} credits
                          </Typography>
                        </Box>
                      </li>
                    )}
                    noOptionsText={
                      courses.length === 0 && !coursesLoading
                        ? "No courses available for registration"
                        : "No matching courses"
                    }
                  />
                )}
              />
            )}
          </Box>
        );

      case 5:
        return (
          <Box>
            <Typography variant="subtitle1" fontWeight={600} gutterBottom>
              Review Your Registration
            </Typography>
            <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
              Please verify your information before submitting
            </Typography>
            <Box
              sx={{
                bgcolor: "grey.50",
                p: 3,
                borderRadius: 1,
                border: "1px solid",
                borderColor: "grey.200",
              }}
            >
              <Box sx={{ mb: 2 }}>
                <Typography variant="subtitle2" color={PRIMARY_COLOR} gutterBottom>
                  Personal Details
                </Typography>
                <Typography variant="body2">
                  {lecturerValues.firstName} {lecturerValues.lastName}
                </Typography>
                <Typography variant="body2" color="text.secondary">
                  {lecturerValues.organization}
                </Typography>
              </Box>
              <Box sx={{ mb: 2 }}>
                <Typography variant="subtitle2" color={PRIMARY_COLOR} gutterBottom>
                  Contact Details
                </Typography>
                <Typography variant="body2">{lecturerValues.email}</Typography>
                <Typography variant="body2" color="text.secondary">
                  {lecturerValues.phoneNumber}
                </Typography>
              </Box>
              <Box sx={{ mb: 2 }}>
                <Typography variant="subtitle2" color={PRIMARY_COLOR} gutterBottom>
                  Professional Information
                </Typography>
                <Typography variant="body2">
                  {lecturerValues.specialization}
                </Typography>
              </Box>
              <Box sx={{ mb: 2 }}>
                <Typography variant="subtitle2" color={PRIMARY_COLOR} gutterBottom>
                  Account Details
                </Typography>
                <Typography variant="body2">
                  Username: {lecturerValues.username}
                </Typography>
              </Box>
              <Box>
                <Typography variant="subtitle2" color={PRIMARY_COLOR} gutterBottom>
                  Course Assignment
                </Typography>
                <Typography variant="body2" fontWeight={500}>
                  {courses.find((c) => c.id === lecturerValues.courseId)?.name ||
                    "Selected course"}
                </Typography>
                {(() => {
                  const course = courses.find((c) => c.id === lecturerValues.courseId);
                  return course ? (
                    <Typography variant="body2" color="text.secondary">
                      {course.code} &middot; {course.duration} months &middot;{" "}
                      {course.credits} credits
                    </Typography>
                  ) : null;
                })()}
              </Box>
            </Box>
          </Box>
        );

      default:
        return null;
    }
  };

  // ─── Render: Success Screen ──────────────────────────────────────────────
  const renderSuccess = () => (
    <Paper elevation={3} sx={{ p: { xs: 3, sm: 5 }, width: "100%", borderRadius: 2, textAlign: "center" }}>
      <CheckCircle sx={{ fontSize: 80, color: "success.main", mb: 2 }} />
      <Typography variant="h4" sx={{ color: PRIMARY_COLOR, fontWeight: 700, mb: 2 }}>
        Registration Successful!
      </Typography>
      <Typography variant="body1" color="text.secondary" sx={{ mb: 1 }}>
        {successMessage}
      </Typography>
      <Typography variant="body2" color="text.secondary" sx={{ mb: 4 }}>
        You can now log in to your account and access the system.
      </Typography>

      {(() => {
        const selectedCourse = courses.find(
          (c) =>
            c.id ===
            (studentForm.watch("courseId") || lecturerForm.watch("courseId"))
        );
        return selectedCourse ? (
          <Box
            sx={{
              bgcolor: "grey.50",
              p: 3,
              borderRadius: 1,
              border: "1px solid",
              borderColor: "grey.200",
              mb: 4,
              textAlign: "left",
            }}
          >
            <Typography variant="subtitle2" color={PRIMARY_COLOR} gutterBottom>
              {role === "Student" ? "Enrolled Course" : "Assigned Course"}
            </Typography>
            <Typography variant="body1" fontWeight={600}>
              {selectedCourse.name}
            </Typography>
            <Typography variant="body2" color="text.secondary">
              {selectedCourse.code} &middot; {selectedCourse.duration} months
            </Typography>
          </Box>
        ) : null;
      })()}

      <Button
        variant="contained"
        size="large"
        sx={{ bgcolor: PRIMARY_COLOR, "&:hover": { bgcolor: "#4a5a1f" } }}
        onClick={() => navigate("/login")}
      >
        Proceed to Login
      </Button>
    </Paper>
  );

  // ─── Main Render ─────────────────────────────────────────────────────────
  if (step === "role-selection") {
    return (
      <Container component="main" maxWidth="md">
        <Box
          sx={{
            minHeight: "100vh",
            display: "flex",
            alignItems: "center",
            justifyContent: "center",
            py: 4,
          }}
        >
          {renderRoleSelection()}
        </Box>
      </Container>
    );
  }

  if (step === "success") {
    return (
      <Container component="main" maxWidth="sm">
        <Box
          sx={{
            minHeight: "100vh",
            display: "flex",
            alignItems: "center",
            justifyContent: "center",
            py: 4,
          }}
        >
          {renderSuccess()}
        </Box>
      </Container>
    );
  }

  const steps = role === "Student" ? STUDENT_STEPS : LECTURER_STEPS;
  const isStudent = role === "Student";
  const handleSubmit = isStudent
    ? studentForm.handleSubmit(handleStudentSubmit)
    : lecturerForm.handleSubmit(handleLecturerSubmit);

  return (
    <Container component="main" maxWidth="sm">
      <Box
        sx={{
          minHeight: "100vh",
          display: "flex",
          alignItems: "center",
          justifyContent: "center",
          py: 4,
        }}
      >
        <Paper elevation={3} sx={{ p: { xs: 3, sm: 4 }, width: "100%", borderRadius: 2 }}>
          <Box sx={{ textAlign: "center", mb: 3 }}>
            <Typography
              variant="h5"
              sx={{ color: PRIMARY_COLOR, fontWeight: 700, mb: 0.5 }}
            >
              {isStudent ? "Student Registration" : "Lecturer Registration"}
            </Typography>
            <Typography variant="body2" color="text.secondary">
              {isStudent
                ? "Enroll in courses and start learning"
                : "Set up your teaching profile"}
            </Typography>
          </Box>

          <Stepper
            activeStep={activeStep}
            alternativeLabel
            sx={{ mb: 4, "& .MuiStepLabel-root .Mui-completed": { color: PRIMARY_COLOR } }}
          >
            {steps.map((label) => (
              <Step key={label}>
                <StepLabel>{label}</StepLabel>
              </Step>
            ))}
          </Stepper>

          {error && (
            <Alert severity="error" sx={{ mb: 3 }}>
              {error}
            </Alert>
          )}

          <form onSubmit={handleSubmit}>
            {isStudent ? renderStudentStep(activeStep) : renderLecturerStep(activeStep)}

            <Box
              sx={{
                display: "flex",
                justifyContent: "space-between",
                mt: 4,
                flexWrap: "wrap",
                gap: 1,
              }}
            >
              <Box sx={{ display: "flex", gap: 1 }}>
                <Button
                  variant="outlined"
                  startIcon={<ArrowBack />}
                  onClick={handleBack}
                >
                  {activeStep === 0 ? "Back to Role Selection" : "Back"}
                </Button>
              </Box>

              <Box sx={{ display: "flex", gap: 1 }}>
                {activeStep > 0 && activeStep < steps.length - 1 && (
                  <Button
                    variant="text"
                    onClick={() => goToStep(steps.length - 1)}
                    disabled={!canProceed()}
                  >
                    Skip to Review
                  </Button>
                )}

                {activeStep === steps.length - 1 ? (
                  <Button
                    type="submit"
                    variant="contained"
                    disabled={loading}
                    sx={{
                      bgcolor: PRIMARY_COLOR,
                      "&:hover": { bgcolor: "#4a5a1f" },
                    }}
                  >
                    {loading ? (
                      <>
                        <CircularProgress size={20} sx={{ mr: 1, color: "white" }} />
                        Registering...
                      </>
                    ) : (
                      "Complete Registration"
                    )}
                  </Button>
                ) : (
                  <Button
                    variant="contained"
                    onClick={handleNext}
                    disabled={!canProceed()}
                    sx={{
                      bgcolor: PRIMARY_COLOR,
                      "&:hover": { bgcolor: "#4a5a1f" },
                    }}
                  >
                    Next
                  </Button>
                )}
              </Box>
            </Box>
          </form>

          <Box sx={{ mt: 3, textAlign: "center" }}>
            <Typography variant="body2" color="text.secondary">
              Already have an account?{" "}
              <Link
                to="/login"
                style={{ color: PRIMARY_COLOR, fontWeight: 600 }}
              >
                Sign in
              </Link>
            </Typography>
          </Box>
        </Paper>
      </Box>
    </Container>
  );
};

export default Register;
