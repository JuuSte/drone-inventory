import { HttpErrorResponse } from '@angular/common/http';

export const fallbackErrorMessage = 'The request failed. Please try again.';

interface ProblemDetails {
  title?: string;
  detail?: string;
}

function problemDetailsOf(error: unknown): ProblemDetails | null {
  if (!(error instanceof HttpErrorResponse)) {
    return null;
  }

  const body: unknown = error.error;
  return typeof body === 'object' && body !== null ? (body as ProblemDetails) : null;
}

/**
 * Extracts the message of a `ProblemDetails` error response, preferring `detail`
 * over `title` because the backend puts the specific rule violation in `detail`.
 */
export function problemDetailsMessage(error: unknown): string {
  const problem = problemDetailsOf(error);
  const detail = typeof problem?.detail === 'string' ? problem.detail.trim() : '';
  if (detail) {
    return detail;
  }

  const title = typeof problem?.title === 'string' ? problem.title.trim() : '';
  if (title) {
    return title;
  }

  return fallbackErrorMessage;
}
