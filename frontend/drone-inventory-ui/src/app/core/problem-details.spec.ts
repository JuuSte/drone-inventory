import { HttpErrorResponse } from '@angular/common/http';
import { fallbackErrorMessage, problemDetailsMessage } from './problem-details';

function errorWith(error: unknown, status = 400): HttpErrorResponse {
  return new HttpErrorResponse({ status, error, url: 'http://localhost:5168/api/drones' });
}

describe('problemDetailsMessage', () => {
  it('should return the detail of a ProblemDetails response', () => {
    const error = errorWith({ title: 'Bad Request', detail: 'Name is required.', status: 400 });

    expect(problemDetailsMessage(error)).toBe('Name is required.');
  });

  it('should fall back to the title when the response has no detail', () => {
    const error = errorWith({ title: 'Drone is already loaned.', status: 409 }, 409);

    expect(problemDetailsMessage(error)).toBe('Drone is already loaned.');
  });

  it('should ignore blank messages', () => {
    const error = errorWith({ title: 'Conflict', detail: '   ', status: 409 }, 409);

    expect(problemDetailsMessage(error)).toBe('Conflict');
  });

  it('should return a generic message for errors without a ProblemDetails body', () => {
    expect(problemDetailsMessage(new Error('boom'))).toBe(fallbackErrorMessage);
    expect(problemDetailsMessage(errorWith(null, 500))).toBe(fallbackErrorMessage);
  });
});
