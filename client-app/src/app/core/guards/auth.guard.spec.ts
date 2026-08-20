import { vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { Router, UrlTree } from '@angular/router';

import { AuthService } from '../services/auth.service';
import { authGuard, guestGuard } from './auth.guard';

describe('auth guards', () => {
  let authService: { isAuthenticated: ReturnType<typeof vi.fn> };
  let router: Router;
  let loginUrlTree: UrlTree;
  let productsUrlTree: UrlTree;

  beforeEach(() => {
    authService = { isAuthenticated: vi.fn() };

    TestBed.configureTestingModule({
      providers: [{ provide: AuthService, useValue: authService }]
    });

    router = TestBed.inject(Router);
    loginUrlTree = router.parseUrl('/login');
    productsUrlTree = router.parseUrl('/products');
  });

  describe('authGuard', () => {
    it('should allow access when authenticated', () => {
      authService.isAuthenticated.mockReturnValue(true);

      const result = TestBed.runInInjectionContext(() => authGuard({} as never, {} as never));

      expect(result).toBe(true);
    });

    it('should redirect to /login when not authenticated', () => {
      authService.isAuthenticated.mockReturnValue(false);

      const result = TestBed.runInInjectionContext(() => authGuard({} as never, {} as never));

      expect((result as UrlTree).toString()).toBe(loginUrlTree.toString());
    });
  });

  describe('guestGuard', () => {
    it('should allow access to the login page when not authenticated', () => {
      authService.isAuthenticated.mockReturnValue(false);

      const result = TestBed.runInInjectionContext(() => guestGuard({} as never, {} as never));

      expect(result).toBe(true);
    });

    it('should redirect an already-authenticated user away from /login to /products', () => {
      authService.isAuthenticated.mockReturnValue(true);

      const result = TestBed.runInInjectionContext(() => guestGuard({} as never, {} as never));

      expect((result as UrlTree).toString()).toBe(productsUrlTree.toString());
    });
  });
});
