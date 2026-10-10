import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { WindowPlacement, rightHalfOfScreen } from '../../shared/window-placement';
import { BrowserStatus } from '../models/browser';

/** The SwiftJob-controlled browser window (Playwright) and the prototype form-filling AI. */
@Injectable({ providedIn: 'root' })
export class BrowserApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/browser';

  status(): Observable<BrowserStatus> {
    return this.http.get<BrowserStatus>(`${this.baseUrl}/status`);
  }

  /** A newly opened browser window is placed on the right half of the screen, beside SwiftJob. */
  openJob(
    jobId: number,
    placement: WindowPlacement = rightHalfOfScreen(),
  ): Observable<BrowserStatus> {
    return this.http.post<BrowserStatus>(`${this.baseUrl}/open-job/${jobId}`, placement);
  }

  openDemoForm(placement: WindowPlacement = rightHalfOfScreen()): Observable<BrowserStatus> {
    return this.http.post<BrowserStatus>(`${this.baseUrl}/demo/open`, placement);
  }

  startDemoFill(placement: WindowPlacement = rightHalfOfScreen()): Observable<BrowserStatus> {
    return this.http.post<BrowserStatus>(`${this.baseUrl}/demo/fill`, placement);
  }

  /** Moves the browser window beside SwiftJob (it is briefly reopened; the page and logins are kept). */
  arrange(placement: WindowPlacement = rightHalfOfScreen()): Observable<BrowserStatus> {
    return this.http.post<BrowserStatus>(`${this.baseUrl}/arrange`, placement);
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
