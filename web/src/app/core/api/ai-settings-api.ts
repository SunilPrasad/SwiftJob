import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { AiSettings, AiTestResult, SaveAiSettings } from '../models/ai';

@Injectable({ providedIn: 'root' })
export class AiSettingsApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/settings/ai';

  get(): Observable<AiSettings> {
    return this.http.get<AiSettings>(this.baseUrl);
  }

  save(settings: SaveAiSettings): Observable<AiSettings> {
    return this.http.put<AiSettings>(this.baseUrl, settings);
  }

  test(): Observable<AiTestResult> {
    return this.http.post<AiTestResult>(`${this.baseUrl}/test`, null);
  }
}
