import { HttpErrorResponse } from '@angular/common/http';

export interface ApiErrorBody {
  message?: string;
  errors?: Record<string, string[]>;
}

export function extractErrorMessage(error: unknown, fallback = 'Something went wrong. Please try again.'): string {
  if (error instanceof HttpErrorResponse) {
    const body = error.error as ApiErrorBody | undefined;

    if (body?.errors) {
      const messages = Object.values(body.errors).flat();
      if (messages.length > 0) {
        return messages.join(' ');
      }
    }

    if (body?.message) {
      return body.message;
    }

    if (error.status === 0) {
      return 'Unable to reach the server. Please check your connection and try again.';
    }
  }

  return fallback;
}
