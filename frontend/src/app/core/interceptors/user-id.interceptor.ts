import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { SessionStore } from '../services/session.store';

// Tells the API who is making the request (no real authentication, see README).
export const userIdInterceptor: HttpInterceptorFn = (request, next) => {
  const user = inject(SessionStore).user();
  return next(user ? request.clone({ setHeaders: { 'X-User-Id': String(user.id) } }) : request);
};
