import { vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { of, throwError } from 'rxjs';

import { AuthService } from '../../../core/services/auth.service';
import { AuthResponse } from '../../../core/models/auth.model';
import { Login } from './login';

describe('Login', () => {
  let authService: { login: ReturnType<typeof vi.fn> };
  let router: Router;

  function createComponent(): Login {
    return TestBed.createComponent(Login).componentInstance;
  }

  beforeEach(async () => {
    authService = { login: vi.fn() };

    await TestBed.configureTestingModule({
      imports: [Login],
      providers: [provideRouter([]), { provide: AuthService, useValue: authService }]
    }).compileComponents();

    router = TestBed.inject(Router);
    vi.spyOn(router, 'navigate').mockResolvedValue(true);
  });

  it('should not submit and should mark all fields as touched when the form is invalid', () => {
    const component = createComponent();

    component.submit();

    expect(authService.login).not.toHaveBeenCalled();
    expect(component.form.get('email')!.touched).toBe(true);
    expect(component.form.get('password')!.touched).toBe(true);
  });

  it('should log in and navigate to /products on success', () => {
    const response: AuthResponse = { token: 'a.b.c', username: 'demo', email: 'demo@example.com' };
    authService.login.mockReturnValue(of(response));
    const component = createComponent();
    component.form.setValue({ email: 'demo@example.com', password: 'Demo@1234' });

    component.submit();

    expect(authService.login).toHaveBeenCalledWith({ email: 'demo@example.com', password: 'Demo@1234' });
    expect(component.isSubmitting()).toBe(false);
    expect(component.errorMessage()).toBeNull();
    expect(router.navigate).toHaveBeenCalledWith(['/products']);
  });

  it('should surface an error message and stop submitting when login fails', () => {
    authService.login.mockReturnValue(
      throwError(() => ({ status: 401, error: { message: 'Invalid email or password.' } }))
    );
    const component = createComponent();
    component.form.setValue({ email: 'demo@example.com', password: 'wrong-password' });

    component.submit();

    expect(component.isSubmitting()).toBe(false);
    expect(component.errorMessage()).toBe('Invalid email or password.');
    expect(router.navigate).not.toHaveBeenCalled();
  });

  it('should not submit twice while a request is already in flight', () => {
    authService.login.mockReturnValue(of({ token: 't', username: 'u', email: 'e@example.com' } as AuthResponse));
    const component = createComponent();
    component.form.setValue({ email: 'demo@example.com', password: 'Demo@1234' });
    component.isSubmitting.set(true);

    component.submit();

    expect(authService.login).not.toHaveBeenCalled();
  });

  it('togglePasswordVisibility() should flip the hidden state and prevent the default click action', () => {
    const component = createComponent();
    const event = { preventDefault: vi.fn() } as unknown as Event;
    expect(component.hidePassword()).toBe(true);

    component.togglePasswordVisibility(event);

    expect(event.preventDefault).toHaveBeenCalled();
    expect(component.hidePassword()).toBe(false);
  });
});
