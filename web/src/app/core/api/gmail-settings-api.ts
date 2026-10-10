import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { GmailSettings, GmailTestResult } from '../models/gmail';

@Injectable({ providedIn: 'root' })
export class GmailSettingsApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/settings/gmail';

  get(): Observable<GmailSettings> {
    return this.http.get<GmailSettings>(this.baseUrl);
  }

  saveCredentials(clientId: string, clientSecret: string): Observable<GmailSettings> {
    return this.http.put<GmailSettings>(`${this.baseUrl}/credentials`, { clientId, clientSecret });
  }

  removeCredentials(): Observable<GmailSettings> {
    return this.http.delete<GmailSettings>(`${this.baseUrl}/credentials`);
  }

  updateSearchQuery(searchQuery: string): Observable<GmailSettings> {
    return this.http.put<GmailSettings>(`${this.baseUrl}/search-query`, { searchQuery });
  }

  /** Returns the Google sign-in URL to navigate to. */
  connect(): Observable<{ authorizationUrl: string }> {
    return this.http.post<{ authorizationUrl: string }>(`${this.baseUrl}/connect`, null);
  }

  disconnect(): Observable<GmailSettings> {
    return this.http.post<GmailSettings>(`${this.baseUrl}/disconnect`, null);
  }

  test(): Observable<GmailTestResult> {
    return this.http.post<GmailTestResult>(`${this.baseUrl}/test`, null);
  }
}
