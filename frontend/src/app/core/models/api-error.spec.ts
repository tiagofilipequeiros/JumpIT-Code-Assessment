import { HttpErrorResponse } from '@angular/common/http';
import { ERROR_MESSAGES, ErrorCode } from '../enums/error-code';
import { ApiError } from './api-error';

describe('ApiError', () => {
  it('reads the error code and detail from ProblemDetails', () => {
    const error = ApiError.from(
      new HttpErrorResponse({ status: 409, error: { code: 'InsufficientStock', detail: 'Not enough.' } }),
    );

    expect(error.code).toBe(ErrorCode.InsufficientStock);
    expect(error.detail).toBe('Not enough.');
    expect(error.message).toBe(ERROR_MESSAGES[ErrorCode.InsufficientStock]);
  });

  it('maps validation errors to camelCase field names', () => {
    const error = ApiError.from(
      new HttpErrorResponse({ status: 400, error: { code: 'ValidationFailed', errors: { Name: ['Too short.'] } } }),
    );

    expect(error.fieldErrors).toEqual({ name: ['Too short.'] });
  });

  it('treats unknown codes as Unexpected', () => {
    const error = ApiError.from(new HttpErrorResponse({ status: 500, error: { code: 'SomethingNew' } }));

    expect(error.code).toBe(ErrorCode.Unexpected);
  });

  it('reports a network error when the API is unreachable', () => {
    const error = ApiError.from(new HttpErrorResponse({ status: 0 }));

    expect(error.code).toBe(ErrorCode.NetworkError);
  });
});
