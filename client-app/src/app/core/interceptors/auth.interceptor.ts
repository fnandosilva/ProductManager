import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Observable, catchError, finalize, shareReplay, switchMap, throwError } from 'rxjs';

import { AuthResponse } from '../models/auth.model';
import { AuthService } from '../services/auth.service';

const AUTH_URL_MARKERS = ['/auth/login', '/auth/refresh', '/auth/revoke'];

let refreshInFlight$: Observable<AuthResponse> | null = null;

export function resetAuthInterceptorStateForTests(): void {
  refreshInFlight$ = null;
}

function isAuthEndpoint(url: string): boolean {
  return AUTH_URL_MARKERS.some((marker) => url.includes(marker));
}

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);
  const isAuthRequest = isAuthEndpoint(req.url);
  const token = isAuthRequest ? null : authService.getToken();

  const authorizedRequest = token
    ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
    : req;

  return next(authorizedRequest).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status !== 401 || isAuthRequest) {
        return throwError(() => error);
      }

      if (!authService.getRefreshToken()) {
        authService.logout();
        return throwError(() => error);
      }

      if (!refreshInFlight$) {
        refreshInFlight$ = authService.refreshToken().pipe(
          finalize(() => {
            setTimeout(() => {
              refreshInFlight$ = null;
            }, 0);
          }),
          shareReplay(1)
        );
      }

      return refreshInFlight$.pipe(
        switchMap((response) => {
          const retried = req.clone({
            setHeaders: { Authorization: `Bearer ${response.token}` }
          });
          return next(retried);
        }),
        catchError((refreshError) => {
          authService.logout();
          return throwError(() => refreshError);
        })
      );
    })
  );
};
