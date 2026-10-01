import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { Permission } from '../enums/permission';
import { SessionStore } from '../services/session.store';

// Route guard: only users with the permission can open the page. The API checks again on every call.
export const permissionGuard =
  (permission: Permission): CanActivateFn =>
  () =>
    inject(SessionStore).can(permission) || inject(Router).createUrlTree(['/']);
