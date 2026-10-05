import { Injectable, signal } from '@angular/core';
import { Permission } from '../enums/permission';
import { User } from '../models/user';

const STORAGE_KEY = 'selectedUserId';

// The currently selected user. No HTTP here, so the interceptor can use it without circular dependencies.
@Injectable({ providedIn: 'root' })
export class SessionStore {
  private readonly currentUser = signal<User | null>(null);

  readonly user = this.currentUser.asReadonly();

  can(permission: Permission): boolean {
    return this.currentUser()?.permissions.includes(permission) ?? false;
  }

  setUser(user: User): void {
    this.currentUser.set(user);
    try {
      localStorage.setItem(STORAGE_KEY, String(user.id));
    } catch {
      // Storage can be unavailable (private mode); the selection just won't be remembered.
    }
  }

  storedUserId(): number | null {
    try {
      const value = Number(localStorage.getItem(STORAGE_KEY));
      return Number.isInteger(value) && value > 0 ? value : null;
    } catch {
      return null;
    }
  }
}
