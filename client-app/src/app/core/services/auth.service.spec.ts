import { vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { Router } from '@angular/router';

import { environment } from '../../../environments/environment';
import { AuthResponse } from '../models/auth.model';
import { AuthService } from './auth.service';

describe('AuthService', () => {
  let httpMock: HttpTestingController;
  let router: { navigate: ReturnType<typeof vi.fn> };

  const baseUrl = `${environment.apiUrl}/auth`;

  function createService(): AuthService {
    TestBed.resetTestingModule();
    router = { navigate: vi.fn() };
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), { provide: Router, useValue: router }]
    });
    httpMock = TestBed.inject(HttpTestingController);
    return TestBed.inject(AuthService);
  }

  beforeEach(() => {
    localStorage.clear();
  });

  afterEach(() => {
    httpMock?.verify();
    localStorage.clear();
  });

  it('should start unauthenticated when nothing is stored', () => {
    const service = createService();

    expect(service.isAuthenticated()).toBe(false);
    expect(service.currentUser()).toBeNull();
    expect(service.getToken()).toBeNull();
    expect(service.getRefreshToken()).toBeNull();
  });

  it('should restore a previously stored session on construction', () => {
    localStorage.setItem('pm_token', 'stored-token');
    localStorage.setItem('pm_refresh_token', 'stored-refresh');
    localStorage.setItem('pm_user', JSON.stringify({ username: 'alice', email: 'alice@example.com' }));

    const service = createService();

    expect(service.isAuthenticated()).toBe(true);
    expect(service.currentUser()).toEqual({ username: 'alice', email: 'alice@example.com' });
    expect(service.getToken()).toBe('stored-token');
    expect(service.getRefreshToken()).toBe('stored-refresh');
  });

  it('should treat corrupted stored user JSON as no session', () => {
    localStorage.setItem('pm_user', '{not-valid-json');

    const service = createService();

    expect(service.currentUser()).toBeNull();
    expect(service.isAuthenticated()).toBe(false);
  });

  it('login() should POST to /auth/login and store the session on success', () => {
    const service = createService();
    const response: AuthResponse = {
      token: 'abc.def.ghi',
      username: 'bob',
      email: 'bob@example.com',
      refreshToken: 'refresh-abc'
    };

    service.login({ email: 'bob@example.com', password: 'Password123!' }).subscribe((result) => {
      expect(result).toEqual(response);
    });

    const req = httpMock.expectOne(`${baseUrl}/login`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ email: 'bob@example.com', password: 'Password123!' });
    req.flush(response);

    expect(service.isAuthenticated()).toBe(true);
    expect(service.currentUser()).toEqual({ username: 'bob', email: 'bob@example.com' });
    expect(service.getToken()).toBe('abc.def.ghi');
    expect(service.getRefreshToken()).toBe('refresh-abc');
    expect(localStorage.getItem('pm_token')).toBe('abc.def.ghi');
    expect(localStorage.getItem('pm_refresh_token')).toBe('refresh-abc');
  });

  it('login() should not establish a session when the request fails', () => {
    const service = createService();

    service.login({ email: 'bob@example.com', password: 'wrong' }).subscribe({
      next: () => expect.fail('expected an error, got a successful response'),
      error: () => {
        /* expected */
      }
    });

    const req = httpMock.expectOne(`${baseUrl}/login`);
    req.flush({ message: 'Invalid credentials' }, { status: 401, statusText: 'Unauthorized' });

    expect(service.isAuthenticated()).toBe(false);
    expect(service.getToken()).toBeNull();
  });

  it('refreshToken() should POST the stored refresh token and replace both tokens', () => {
    const service = createService();
    localStorage.setItem('pm_refresh_token', 'old-refresh');
    const response: AuthResponse = {
      token: 'new-access',
      username: 'bob',
      email: 'bob@example.com',
      refreshToken: 'new-refresh'
    };

    service.refreshToken().subscribe((result) => {
      expect(result).toEqual(response);
    });

    const req = httpMock.expectOne(`${baseUrl}/refresh`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ refreshToken: 'old-refresh' });
    req.flush(response);

    expect(service.getToken()).toBe('new-access');
    expect(service.getRefreshToken()).toBe('new-refresh');
  });

  it('logout() should clear the session, navigate to /login, and revoke the refresh token', () => {
    localStorage.setItem('pm_token', 'some-token');
    localStorage.setItem('pm_refresh_token', 'refresh-abc');
    localStorage.setItem('pm_user', JSON.stringify({ username: 'carol', email: 'carol@example.com' }));
    const service = createService();
    expect(service.isAuthenticated()).toBe(true);

    service.logout();

    expect(service.isAuthenticated()).toBe(false);
    expect(service.currentUser()).toBeNull();
    expect(localStorage.getItem('pm_token')).toBeNull();
    expect(localStorage.getItem('pm_refresh_token')).toBeNull();
    expect(localStorage.getItem('pm_user')).toBeNull();
    expect(router.navigate).toHaveBeenCalledWith(['/login']);

    const req = httpMock.expectOne(`${baseUrl}/revoke`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ refreshToken: 'refresh-abc' });
    req.flush({});
  });
});
