import { Injectable, inject } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ERROR_MESSAGES, ErrorCode } from '../enums/error-code';
import { FeedbackMessage } from '../enums/feedback-message';
import { ApiError } from '../models/api-error';

const SUCCESS_DURATION_MS = 3000;
const ERROR_DURATION_MS = 6000;

// All user feedback goes through here, so messages look and behave the same everywhere.
@Injectable({ providedIn: 'root' })
export class NotificationService {
  private readonly snackBar = inject(MatSnackBar);

  success(message: FeedbackMessage, suffix = ''): void {
    this.snackBar.open(suffix ? `${message} ${suffix}` : message, 'OK', {
      duration: SUCCESS_DURATION_MS,
    });
  }

  error(error: unknown): void {
    const message =
      error instanceof ApiError ? error.message : ERROR_MESSAGES[ErrorCode.Unexpected];
    this.snackBar.open(message, 'Close', {
      duration: ERROR_DURATION_MS,
      panelClass: 'snackbar-error',
    });
  }
}
