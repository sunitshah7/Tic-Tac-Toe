import { InjectionToken } from '@angular/core';
import { environment } from '../../environments/environment';

/**
 * Base URL of the .NET API, injected rather than imported directly so tests can point the
 * service at a stub without touching the environment file.
 */
export const API_BASE_URL = new InjectionToken<string>('API_BASE_URL', {
  providedIn: 'root',
  factory: () => environment.apiBaseUrl,
});
