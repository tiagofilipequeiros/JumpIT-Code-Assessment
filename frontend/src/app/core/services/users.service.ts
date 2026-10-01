import { HttpClient } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { Observable, catchError, of, switchMap, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { User } from '../models/user';
import { SessionStore } from './session.store';

@Injectable({ providedIn: 'root' })
export class UsersService {
  private readonly http = inject(HttpClient);
  private readonly session = inject(SessionStore);

  // Users available in the header dropdown.
  readonly users = signal<User[]>([]);

  // Picking a user in the dropdown counts as a login (recorded in the metrics).
  login(email: string): Observable<User> {
    return this.http
      .post<User>(`${environment.apiUrl}/auth/login`, { email })
      .pipe(tap((user) => this.session.setUser(user)));
  }

  // On startup: load the users and sign in as the last selected one (or the first).
  restoreSession(): Observable<unknown> {
    return this.http.get<User[]>(`${environment.apiUrl}/users`).pipe(
      tap((users) => this.users.set(users)),
      switchMap((users) => {
        const user = users.find((u) => u.id === this.session.storedUserId()) ?? users[0];
        return user ? this.login(user.email) : of(null);
      }),
      // The app still starts if the API is down; pages show the error.
      catchError(() => of(null)),
    );
  }
}
