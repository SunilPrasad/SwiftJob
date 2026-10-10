import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { BrowserStatus } from '../models/browser';

/** The SwiftJob-controlled browser window (Playwright) and the prototype form-filling AI. */
@Injectable({ providedIn: 'root' })
export class BrowserApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/browser';

  status(): Observable<BrowserStatus> {
    return this.http.get<BrowserStatus>(`${this.baseUrl}/status`);
  }

  openJob(jobId: number): Observable<BrowserStatus> {
    return this.http.post<BrowserStatus>(`${this.baseUrl}/open-job/${jobId}`, null);
  }

  openDemoForm(): Observable<BrowserStatus> {
    return this.http.post<BrowserStatus>(`${this.baseUrl}/demo/open`, null);
  }

  startDemoFill(): Observable<BrowserStatus> {
    return this.http.post<BrowserStatus>(`${this.baseUrl}/demo/fill`, null);
  }

  takeControl(): Observable<BrowserStatus> {
    return this.http.post<BrowserStatus>(`${this.baseUrl}/agent/take-control`, null);
  }

  resume(): Observable<BrowserStatus> {
    return this.http.post<BrowserStatus>(`${this.baseUrl}/agent/resume`, null);
  }

  stop(): Observable<BrowserStatus> {
    return this.http.post<BrowserStatus>(`${this.baseUrl}/agent/stop`, null);
  }

  close(): Observable<BrowserStatus> {
    return this.http.post<BrowserStatus>(`${this.baseUrl}/close`, null);
  }
}
