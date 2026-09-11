import { vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { of, Subject, throwError } from 'rxjs';

import { AuthResponse } from '../models/auth.model';
import { AuthService } from '../services/auth.service';
import { authInterceptor, resetAuthInterceptorStateForTests } from './auth.interceptor';

describe('authInterceptor', () => {
  let http: HttpClient;
  let httpMock: HttpTestingController;
  let authService: {
    getToken: ReturnType<typeof vi.fn>;
    getRefreshToken: ReturnType<typeof vi.fn>;
    logout: ReturnType<typeof vi.fn>;
    refreshToken: ReturnType<typeof vi.fn>;
  };

  beforeEach(() => {
    resetAuthInterceptorStateForTests();
    authService = {
      getToken: vi.fn().mockReturnValue(null),
      getRefreshToken: vi.fn().mockReturnValue(null),
      logout: vi.fn(),
      refreshToken: vi.fn()
    };

    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
        { provide: AuthService, useValue: authService }
      ]
    });

    http = TestBed.inject(HttpClient);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
    resetAuthInterceptorStateForTests();
  });

  it('should not add an Authorization header when there is no token', () => {
    http.get('/api/products').subscribe();

    const req = httpMock.expectOne('/api/products');
    expect(req.request.headers.has('Authorization')).toBe(false);
    req.flush([]);
  });

  it('should attach a Bearer Authorization header when a token is present', () => {
    authService.getToken.mockReturnValue('my-jwt-token');

    http.get('/api/products').subscribe();

    const req = httpMock.expectOne('/api/products');
    expect(req.request.headers.get('Authorization')).toBe('Bearer my-jwt-token');
    req.flush([]);
  });

  it('should not attach an Authorization header to auth endpoints', () => {
    authService.getToken.mockReturnValue('my-jwt-token');

    http.post('/api/auth/refresh', { refreshToken: 'r' }).subscribe();

    const req = httpMock.expectOne('/api/auth/refresh');
    expect(req.request.headers.has('Authorization')).toBe(false);
    req.flush({});
  });

  it('should refresh and retry the original request on a 401 when a refresh token exists', () => {
    authService.getToken.mockReturnValue('expired-token');
    authService.getRefreshToken.mockReturnValue('refresh-plain');
    authService.refreshToken.mockReturnValue(
      of({
        token: 'new-jwt',
        username: 'bob',
        email: 'bob@example.com',
        refreshToken: 'new-refresh'
      })
    );

    http.get('/api/products').subscribe((data) => {
      expect(data).toEqual([]);
    });

    const req = httpMock.expectOne('/api/products');
    expect(req.request.headers.get('Authorization')).toBe('Bearer expired-token');
    req.flush({ message: 'Unauthorized' }, { status: 401, statusText: 'Unauthorized' });

    const retry = httpMock.expectOne('/api/products');
    expect(retry.request.headers.get('Authorization')).toBe('Bearer new-jwt');
    retry.flush([]);

    expect(authService.refreshToken).toHaveBeenCalledTimes(1);
    expect(authService.logout).not.toHaveBeenCalled();
  });

  it('should only call refresh once when multiple requests 401 concurrently', () => {
    const refresh$ = new Subject<AuthResponse>();
    authService.getToken.mockReturnValue('expired-token');
    authService.getRefreshToken.mockReturnValue('refresh-plain');
    authService.refreshToken.mockReturnValue(refresh$.asObservable());

    http.get('/api/products').subscribe();
    http.get('/api/products/100001').subscribe();

    httpMock.expectOne('/api/products').flush({ message: 'Unauthorized' }, { status: 401, statusText: 'Unauthorized' });
    httpMock
      .expectOne('/api/products/100001')
      .flush({ message: 'Unauthorized' }, { status: 401, statusText: 'Unauthorized' });

    expect(authService.refreshToken).toHaveBeenCalledTimes(1);

    refresh$.next({
      token: 'new-jwt',
      username: 'bob',
      email: 'bob@example.com',
      refreshToken: 'new-refresh'
    });
    refresh$.complete();

    httpMock.expectOne('/api/products').flush([]);
    httpMock.expectOne('/api/products/100001').flush({});

    expect(authService.logout).not.toHaveBeenCalled();
  });

  it('should log out when a 401 occurs and there is no refresh token', () => {
    authService.getToken.mockReturnValue('expired-token');
    authService.getRefreshToken.mockReturnValue(null);

    http.get('/api/products').subscribe({
      next: () => expect.fail('expected the request to error'),
      error: (error) => {
        expect(error.status).toBe(401);
      }
    });

    const req = httpMock.expectOne('/api/products');
    req.flush({ message: 'Unauthorized' }, { status: 401, statusText: 'Unauthorized' });

    expect(authService.refreshToken).not.toHaveBeenCalled();
    expect(authService.logout).toHaveBeenCalledTimes(1);
  });

  it('should log out when refresh itself fails', () => {
    authService.getToken.mockReturnValue('expired-token');
    authService.getRefreshToken.mockReturnValue('refresh-plain');
    authService.refreshToken.mockReturnValue(throwError(() => ({ status: 401 })));

    http.get('/api/products').subscribe({
      next: () => expect.fail('expected the request to error'),
      error: () => {
        /* expected */
      }
    });

    const req = httpMock.expectOne('/api/products');
    req.flush({ message: 'Unauthorized' }, { status: 401, statusText: 'Unauthorized' });

    expect(authService.logout).toHaveBeenCalledTimes(1);
  });

  it('should not attempt refresh on a 401 from login, refresh, or revoke endpoints', () => {
    authService.getRefreshToken.mockReturnValue('refresh-plain');

    http.post('/api/auth/login', {}).subscribe({
      next: () => expect.fail('expected the request to error'),
      error: () => {
        /* expected */
      }
    });

    const req = httpMock.expectOne('/api/auth/login');
    req.flush({ message: 'Invalid credentials' }, { status: 401, statusText: 'Unauthorized' });

    expect(authService.refreshToken).not.toHaveBeenCalled();
    expect(authService.logout).not.toHaveBeenCalled();
  });

  it('should pass non-401 errors through without logging out', () => {
    http.get('/api/products').subscribe({
      next: () => expect.fail('expected the request to error'),
      error: (error) => {
        expect(error.status).toBe(500);
      }
    });

    const req = httpMock.expectOne('/api/products');
    req.flush({ message: 'Server error' }, { status: 500, statusText: 'Internal Server Error' });

    expect(authService.logout).not.toHaveBeenCalled();
  });
});
