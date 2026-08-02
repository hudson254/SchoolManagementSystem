import React, { useState } from "react";
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
} from "@mui/material";
import {
  Visibility,
  VisibilityOff,
  Email,
  Lock,
  Person,
  Phone,
} from "@mui/icons-material";
import { useForm, Controller } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { useAuth } from "../hooks/useAuth";

const registerSchema = z
  .object({
    firstName: z.string().min(1, "First name is required"),
    lastName: z.string().min(1, "Last name is required"),
    email: z.string().email("Invalid email address"),
    password: z.string().min(8, "Password must be at least 8 characters"),
    confirmPassword: z.string().min(1, "Please confirm your password"),
    phoneNumber: z.string().optional(),
  })
  .refine((data) => data.password === data.confirmPassword, {
    message: "Passwords do not match",
    path: ["confirmPassword"],
  });

type RegisterFormData = z.infer<typeof registerSchema>;

const steps = ["Account Details", "Personal Information", "Review"];

export const Register: React.FC = () => {
  const [activeStep, setActiveStep] = useState(0);
  const [showPassword, setShowPassword] = useState(false);
  const [showConfirmPassword, setShowConfirmPassword] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);
  const navigate = useNavigate();
  const { register: registerUser } = useAuth();

  const {
    control,
    handleSubmit,
    watch,
    formState: { errors },
  } = useForm<RegisterFormData>({
    resolver: zodResolver(registerSchema),
    defaultValues: {
      firstName: "",
      lastName: "",
      email: "",
      password: "",
      confirmPassword: "",
      phoneNumber: "",
    },
  });

  const values = watch();

  const handleNext = () => {
    setActiveStep((prev) => Math.min(prev + 1, steps.length - 1));
  };

  const handleBack = () => {
    setActiveStep((prev) => Math.max(prev - 1, 0));
  };

  const onSubmit = async (data: RegisterFormData) => {
    setError(null);
    setLoading(true);

    try {
      await registerUser({
        firstName: data.firstName,
        lastName: data.lastName,
        email: data.email,
        password: data.password,
        confirmPassword: data.confirmPassword,
        phoneNumber: data.phoneNumber || "",
        organization: "",
      });
      navigate("/dashboard");
    } catch (err: any) {
      setError(
        err.response?.data?.message || "Registration failed. Please try again.",
      );
    } finally {
      setLoading(false);
    }
  };

  const renderStepContent = (step: number) => {
    switch (step) {
      case 0:
        return (
          <Box>
            <Controller
              name="email"
              control={control}
              render={({ field }) => (
                <TextField
                  {...field}
                  fullWidth
                  label="Email Address"
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
              name="password"
              control={control}
              render={({ field }) => (
                <TextField
                  {...field}
                  fullWidth
                  label="Password"
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
                        >
                          {showPassword ? <VisibilityOff /> : <Visibility />}
                        </IconButton>
                      </InputAdornment>
                    ),
                  }}
                />
              )}
            />
            <Controller
              name="confirmPassword"
              control={control}
              render={({ field }) => (
                <TextField
                  {...field}
                  fullWidth
                  label="Confirm Password"
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
                          onClick={() =>
                            setShowConfirmPassword(!showConfirmPassword)
                          }
                          edge="end"
                        >
                          {showConfirmPassword ? (
                            <VisibilityOff />
                          ) : (
                            <Visibility />
                          )}
                        </IconButton>
                      </InputAdornment>
                    ),
                  }}
                />
              )}
            />
          </Box>
        );
      case 1:
        return (
          <Box>
            <Controller
              name="firstName"
              control={control}
              render={({ field }) => (
                <TextField
                  {...field}
                  fullWidth
                  label="First Name"
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
                  label="Last Name"
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
              name="phoneNumber"
              control={control}
              render={({ field }) => (
                <TextField
                  {...field}
                  fullWidth
                  label="Phone Number (Optional)"
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
          <Box sx={{ mt: 2 }}>
            <Typography variant="subtitle1" fontWeight={600} gutterBottom>
              Review Your Information
            </Typography>
            <Box sx={{ bgcolor: "grey.50", p: 2, borderRadius: 1, mb: 2 }}>
              <Typography variant="body2" color="textSecondary">
                Email:
              </Typography>
              <Typography variant="body1" gutterBottom>
                {values.email}
              </Typography>
              <Typography variant="body2" color="textSecondary">
                Name:
              </Typography>
              <Typography variant="body1" gutterBottom>
                {values.firstName} {values.lastName}
              </Typography>
              <Typography variant="body2" color="textSecondary">
                Phone:
              </Typography>
              <Typography variant="body1">
                {values.phoneNumber || "Not provided"}
              </Typography>
            </Box>
            <Typography variant="caption" color="textSecondary">
              By creating an account, you agree to our Terms of Service and
              Privacy Policy.
            </Typography>
          </Box>
        );
      default:
        return null;
    }
  };

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
        <Paper elevation={3} sx={{ p: 4, width: "100%", borderRadius: 2 }}>
          <Box sx={{ textAlign: "center", mb: 4 }}>
            <Typography
              variant="h4"
              sx={{ color: "primary.main", fontWeight: 700, mb: 1 }}
            >
              Create Account
            </Typography>
            <Typography variant="body2" color="textSecondary">
              Join the School Management System
            </Typography>
          </Box>

          <Stepper activeStep={activeStep} alternativeLabel sx={{ mb: 4 }}>
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

          <form onSubmit={handleSubmit(onSubmit)}>
            {renderStepContent(activeStep)}

            <Box
              sx={{ display: "flex", justifyContent: "space-between", mt: 4 }}
            >
              <Button
                variant="outlined"
                onClick={
                  activeStep === 0 ? () => navigate("/login") : handleBack
                }
              >
                {activeStep === 0 ? "Back to Login" : "Back"}
              </Button>
              {activeStep === steps.length - 1 ? (
                <Button type="submit" variant="contained" disabled={loading}>
                  {loading ? "Creating Account..." : "Create Account"}
                </Button>
              ) : (
                <Button variant="contained" onClick={handleNext}>
                  Next
                </Button>
              )}
            </Box>
          </form>

          <Box sx={{ mt: 3, textAlign: "center" }}>
            <Typography variant="body2" color="textSecondary">
              Already have an account?{" "}
              <Link to="/login" style={{ color: "#576426", fontWeight: 600 }}>
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
