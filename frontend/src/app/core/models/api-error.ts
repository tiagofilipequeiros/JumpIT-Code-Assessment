import { HttpErrorResponse } from '@angular/common/http';
import { ERROR_MESSAGES, ErrorCode } from '../enums/error-code';

// Every failed API call is turned into this (see apiErrorInterceptor).
export class ApiError extends Error {
  constructor(
    readonly status: number,
    readonly code: ErrorCode,
    readonly detail: string | null,
    // Field name (camelCase) -> messages, for validation errors.
    readonly fieldErrors: Record<string, string[]>,
  ) {
    super(ERROR_MESSAGES[code]);
  }

  static from(response: HttpErrorResponse): ApiError {
    // 0: no answer at all; 502-504: nginx answered but the API behind it did not.
    if (response.status === 0 || [502, 503, 504].includes(response.status)) {
      return new ApiError(response.status, ErrorCode.NetworkError, null, {});
    }

    const body = response.error ?? {};
    const code = Object.values(ErrorCode).includes(body.code)
      ? (body.code as ErrorCode)
      : ErrorCode.Unexpected;
    const fieldErrors: Record<string, string[]> = {};
    for (const [field, messages] of Object.entries<string[]>(body.errors ?? {})) {
      fieldErrors[field.charAt(0).toLowerCase() + field.slice(1)] = messages;
    }

    return new ApiError(response.status, code, body.detail ?? null, fieldErrors);
  }
}
