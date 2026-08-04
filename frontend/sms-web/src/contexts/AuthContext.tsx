import React, { createContext, useContext, useState, useEffect, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import { apiClient } from '../services/api';
import { storage } from '../utils/storage';

interface User {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  roles: string[];
  tenantId: string;
  permissions: string[];
  isEmailVerified?: boolean;
  isActive?: boolean;
  lastLoginDate?: string;
  createdAt?: string;
}

interface AuthContextType {
  user: User | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  login: (email: string, password: string, rememberMe?: boolean) => Promise<void>;
  register: (data: RegisterData) => Promise<void>;
  logout: () => void;
  refreshToken: () => Promise<string>;
  changePassword: (currentPassword: string, newPassword: string) => Promise<void>;
  forgotPassword: (email: string) => Promise<void>;
  resetPassword: (email: string, token: string, newPassword: string) => Promise<void>;
  verifyEmail: (userId: string, token: string) => Promise<void>;
  resendVerification: (email: string) => Promise<void>;
}

interface RegisterData {
  firstName: string;
  lastName: string;
  email: string;
  password: string;
  confirmPassword: string;
  phoneNumber: string;
  organization: string;
  role?: string;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export const AuthProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [user, setUser] = useState<User | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const navigate = useNavigate();

  const loadUser = useCallback(async () => {
    try {
      // RISK-08: With httpOnly-cookie authentication, we simply call the
      // /auth/me endpoint; the backend reads the access_token cookie and
      // resolves the user. No token is read from browser storage.
      const response = await apiClient.get('/auth/me');
      setUser(response.data);
      storage.setUser(response.data);
    } catch (error) {
      // 401 means no valid session cookie — silently clear any cached user.
      console.error('Failed to load user:', error);
      storage.clearTokens();
      setUser(null);
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    loadUser();
  }, [loadUser]);

  const login = async (email: string, password: string, rememberMe = false) => {
    try {
      const response = await apiClient.post('/auth/login', {
        email,
        password,
        rememberMe
      });

      // Tokens are set as httpOnly cookies by the backend; the body only
      // carries the non-token user profile fields.
      setUser(response.data);
      storage.setUser(response.data);

      navigate('/dashboard');
    } catch (error) {
      console.error('Login failed:', error);
      throw error;
    }
  };

  const register = async (data: RegisterData) => {
    try {
      const response = await apiClient.post('/auth/register', data);

      // Tokens are set as httpOnly cookies by the backend.
      setUser(response.data);
      storage.setUser(response.data);

      navigate('/dashboard');
    } catch (error) {
      console.error('Registration failed:', error);
      throw error;
    }
  };

  const logout = () => {
    // POST /auth/logout clears the httpOnly auth cookies server-side and
    // revokes the tokens (RISK-05). Fire-and-forget; the local session is
    // cleared immediately regardless of network outcome.
    apiClient.post('/auth/logout').catch((error) => {
      console.error('Logout API call failed:', error);
    });

    storage.clearTokens();
    setUser(null);
    navigate('/login');
  };

  const refreshToken = async (): Promise<string> => {
    try {
      // The backend reads the refresh_token cookie, validates it, rotates it,
      // and sets new httpOnly cookies. Nothing to send in the body.
      const response = await apiClient.post('/auth/refresh-token');

      const token = response.data?.accessToken || '';
      if (!token) {
        throw new Error('No access token returned from refresh');
      }

      return token;
    } catch (error) {
      console.error('Token refresh failed:', error);
      storage.clearTokens();
      setUser(null);
      navigate('/login');
      throw error;
    }
  };

  const changePassword = async (currentPassword: string, newPassword: string) => {
    await apiClient.post('/auth/change-password', {
      currentPassword,
      newPassword
    });
  };

  const forgotPassword = async (email: string) => {
    await apiClient.post('/auth/forgot-password', { email });
  };

  const resetPassword = async (email: string, token: string, newPassword: string) => {
    await apiClient.post('/auth/reset-password', {
      email,
      token,
      newPassword
    });
  };

  const verifyEmail = async (userId: string, token: string) => {
    await apiClient.get(`/auth/verify-email?userId=${userId}&token=${encodeURIComponent(token)}`);
  };

  const resendVerification = async (email: string) => {
    await apiClient.post('/auth/resend-verification', { email });
  };

  const value: AuthContextType = {
    user,
    isAuthenticated: !!user,
    isLoading,
    login,
    register,
    logout,
    refreshToken,
    changePassword,
    forgotPassword,
    resetPassword,
    verifyEmail,
    resendVerification
  };

  return (
    <AuthContext.Provider value={value}>
      {children}
    </AuthContext.Provider>
  );
};

export const useAuth = () => {
  const context = useContext(AuthContext);
  if (context === undefined) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
};
