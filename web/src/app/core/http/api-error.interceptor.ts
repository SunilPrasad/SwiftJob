import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { catchError, throwError } from 'rxjs';

/** Shows a short, user-friendly message for failed API calls, then re-throws so callers can react. */
export const apiErrorInterceptor: HttpInterceptorFn = (req, next) => {
  const snackBar = inject(MatSnackBar);
  return next(req).pipe(
    catchError((error: unknown) => {
      if (error instanceof HttpErrorResponse) {
        snackBar.open(describeHttpError(error), 'Dismiss', { duration: 6000 });
      }
      return throwError(() => error);
    }),
  );
};

export function describeHttpError(error: HttpErrorResponse): string {
  if (error.status === 0) {
    return 'Cannot reach the SwiftJob API. Is the backend running?';
  }
  const problem = error.error as { title?: string; detail?: string } | null;
  return problem?.detail ?? problem?.title ?? `Request failed (${error.status})`;
}
