import { provideHttpClient, withInterceptors } from '@angular/common/http';
import {
  ApplicationConfig,
  inject,
  provideAppInitializer,
  provideBrowserGlobalErrorListeners,
} from '@angular/core';
import { MatIconRegistry } from '@angular/material/icon';
import { provideRouter, withComponentInputBinding } from '@angular/router';
import { routes } from './app.routes';
import { apiErrorInterceptor } from './core/interceptors/api-error.interceptor';
import { userIdInterceptor } from './core/interceptors/user-id.interceptor';
import { UsersService } from './core/services/users.service';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes, withComponentInputBinding()),
    provideHttpClient(withInterceptors([userIdInterceptor, apiErrorInterceptor])),
    provideAppInitializer(() => {
      // index.html loads the Material Symbols font.
      inject(MatIconRegistry).setDefaultFontSetClass('material-symbols-outlined');
      return inject(UsersService).restoreSession();
    }),
  ],
};
