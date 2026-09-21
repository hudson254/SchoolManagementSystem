// Enterprise error normalization utility.
// Maps API error codes to user-friendly, actionable messages.
// Detects offline/network conditions. Ensures users never see
// raw stack traces or technical implementation details.

export interface ApiError {
  success: boolean;
  code: string;
  message: string;
  statusCode?: number;
  correlationId?: string;
  errors?: Record<string, string[]>;
  path?: string;
}

export interface NormalizedError {
  message: string;
  code: string;
  correlationId?: string;
  errors?: Record<string, string[]>;
  isNetworkError: boolean;
  isTimeout: boolean;
  statusCode?: number;
  /**
   * Raw server-provided message when it is more specific than the canned
   * code message (e.g. a concrete business-rule violation such as "A submitted
   * order must contain at least one item."). Callers may prefer this for
   * display. Optional — absent for network/offline/unknown errors.
   */
  serverMessage?: string;
}

// User-friendly fallback messages (matches backend ErrorMessages)
const ERROR_MESSAGES: Record<string, string> = {
  VALIDATION_ERROR: 'Please correct the highlighted fields and try again.',
  INVALID_CREDENTIALS: 'The email or password you entered is incorrect. Please check your credentials and try again.',
  ACCESS_DENIED: 'You do not have permission to perform this action. Please contact your administrator.',
  SESSION_EXPIRED: 'Your session has expired. Please log in again to continue.',
  TOKEN_EXPIRED: 'Your session has expired. Please log in again to continue.',
  TOKEN_INVALID: 'Your authentication token is invalid. Please log in again.',
  ACCOUNT_LOCKED: 'Your account has been temporarily locked due to multiple failed login attempts. Please try again in 15 minutes.',
  ACCOUNT_DISABLED: 'Your account has been disabled. Please contact your administrator for assistance.',
  NOT_FOUND: 'The requested record was not found. It may have been deleted or you may not have permission to view it.',
  DUPLICATE_RECORD: 'A record with this information already exists. Please use unique values and try again.',
  RECORD_IN_USE: 'This record is currently in use and cannot be deleted. Please remove all references first.',
  BUSINESS_RULE_VIOLATION: 'This operation violates a business rule. Please review the details and try again.',
  DATABASE_UNAVAILABLE: 'The database is currently unavailable. Please try again later. If the problem persists, contact your system administrator.',
  NETWORK_UNAVAILABLE: 'A network error occurred. Please check your internet connection and try again.',
  TIMEOUT_ERROR: 'The operation timed out. Please try again. If the problem persists, contact support.',
  EXTERNAL_SERVICE_ERROR: 'An external service is currently unavailable. Please try again later.',
  FILE_SYSTEM_ERROR: 'A file operation failed. Please try again or contact support if the problem persists.',
  UPLOAD_FAILED: 'The file upload failed. Please check the file and try again.',
  DOWNLOAD_FAILED: 'The file download failed. Please try again.',
  SAVE_FAILED: 'Unable to save your changes. Please try again. If the problem persists, contact support.',
  OPERATION_FAILED: 'The operation failed. Please try again. If the problem persists, contact support.',
  REPORT_GENERATION_FAILED: 'Unable to generate the report. Please try again. If the problem persists, contact support.',
  EXPORT_FAILED: 'Unable to export the data. Please try again.',
  IMPORT_FAILED: 'Unable to import the data. Please check the file format and try again.',
  INTERNAL_ERROR: 'An unexpected error occurred while processing your request. Please try again later. If the problem persists, contact support.',
  SERVICE_UNAVAILABLE: 'The service is temporarily unavailable. Please try again in a few minutes.',
  CONFIGURATION_ERROR: 'A configuration error occurred. Please contact your system administrator.',
  FORBIDDEN: 'Access denied. You do not have permission to perform this action.',
  UNAUTHORIZED: 'You are not authorized to perform this action. Please log in and try again.',
};

const DEFAULT_MESSAGE = 'Something went wrong while processing your request. Please try again.';

/**
 * Checks if the browser is currently offline.
 */
export function isOffline(): boolean {
  return typeof navigator !== 'undefined' && navigator.onLine === false;
}

/**
 * Extracts and normalizes an error from an API response or thrown error.
 * Returns a clean, user-friendly message. Never exposes technical details.
 */
export function normalizeError(error: unknown): NormalizedError {
  // Offline detection
  if (isOffline()) {
    return {
      message: 'You are currently offline. Please check your internet connection and try again.',
      code: 'NETWORK_OFFLINE',
      isNetworkError: true,
      isTimeout: false,
    };
  }

  // Axios timeout error — must be checked before isApiErrorShape
  // because { code: 'ECONNABORTED' } matches the API error shape.
  if (error && typeof error === 'object' && (error as any).code === 'ECONNABORTED') {
    return {
      message: ERROR_MESSAGES.TIMEOUT_ERROR,
      code: 'TIMEOUT_ERROR',
      isNetworkError: false,
      isTimeout: true,
    };
  }

  // Network error (no response)
  if (error && typeof error === 'object' && (error as any).code === 'ERR_NETWORK') {
    return {
      message: ERROR_MESSAGES.NETWORK_UNAVAILABLE,
      code: 'NETWORK_ERROR',
      isNetworkError: true,
      isTimeout: false,
    };
  }

  // Axios error carrying the structured API error payload (produced by
  // ExceptionHandlingMiddleware). Prefer the payload over the Axios wrapper,
  // whose own code/message (e.g. ERR_BAD_REQUEST / "Request failed with
  // status code 400") would otherwise mask the server's error taxonomy.
  // (Phase 2D minimal defect fix — the direct-payload behavior below and all
  // existing messages are unchanged.)
  const axiosLike = error as { response?: { data?: unknown } } | null;
  if (axiosLike && typeof axiosLike === 'object' && axiosLike.response && typeof axiosLike.response === 'object') {
    const payload = axiosLike.response.data;
    if (
      payload !== null &&
      typeof payload === 'object' &&
      ('success' in payload || 'code' in payload || 'statusCode' in payload)
    ) {
      return normalizeApiErrorShape(payload as ApiError & { message?: string; serverMessage?: string });
    }
  }

  // Axios-style error
  if (isApiErrorShape(error)) {
    return normalizeApiErrorShape(error as ApiError & { message?: string; serverMessage?: string });
  }

  // HTTP error with status
  if (error && typeof error === 'object' && 'status' in error) {
    const status = (error as any).status as number;
    if (status === 401) {
      return {
        message: ERROR_MESSAGES.SESSION_EXPIRED,
        code: 'SESSION_EXPIRED',
        isNetworkError: false,
        isTimeout: false,
        statusCode: status,
      };
    }
    if (status === 403) {
      return {
        message: ERROR_MESSAGES.ACCESS_DENIED,
        code: 'ACCESS_DENIED',
        isNetworkError: false,
        isTimeout: false,
        statusCode: status,
      };
    }
    if (status === 404) {
      return {
        message: ERROR_MESSAGES.NOT_FOUND,
        code: 'NOT_FOUND',
        isNetworkError: false,
        isTimeout: false,
        statusCode: status,
      };
    }
    if (status === 408 || status === 504) {
      return {
        message: ERROR_MESSAGES.TIMEOUT_ERROR,
        code: 'TIMEOUT_ERROR',
        isNetworkError: false,
        isTimeout: true,
        statusCode: status,
      };
    }
  }

  // Unknown error — show generic user-friendly message, never raw stack
  return {
    message: DEFAULT_MESSAGE,
    code: 'INTERNAL_ERROR',
    isNetworkError: false,
    isTimeout: false,
  };
}

function isApiErrorShape(error: unknown): boolean {
  return (
    error !== null &&
    typeof error === 'object' &&
    ('success' in error || 'code' in error || 'statusCode' in error)
  );
}

/**
 * Maps a structured API error payload (ErrorResponse from
 * ExceptionHandlingMiddleware) to a NormalizedError. Preserves the raw server
 * message when it differs from the canned code message so callers can show
 * the specific business-rule text (Phase 2D — additive; canned messages are
 * unchanged for existing callers).
 */
function normalizeApiErrorShape(apiError: ApiError & { message?: string; serverMessage?: string }): NormalizedError {
  const code = apiError.code || 'INTERNAL_ERROR';
  const message = ERROR_MESSAGES[code] || apiError.message || DEFAULT_MESSAGE;

  const rawServerMessage =
    apiError.serverMessage ??
    (apiError.message && apiError.message !== message ? apiError.message : undefined);

  return {
    message,
    code,
    correlationId: apiError.correlationId,
    errors: apiError.errors,
    isNetworkError: false,
    isTimeout: false,
    statusCode: apiError.statusCode,
    serverMessage: rawServerMessage || undefined,
  };
}

/**
 * Extracts field-level validation errors for display on forms.
 * Reads the errors directly from the raw error object so it is not
 * affected by offline/network short-circuits in normalizeError.
 */
export function getFieldErrors(error: unknown): Record<string, string[]> | undefined {
  if (error && typeof error === 'object' && 'errors' in error) {
    const errors = (error as { errors?: unknown }).errors;
    if (errors && typeof errors === 'object') {
      return errors as Record<string, string[]>;
    }
  }
  return undefined;
}
