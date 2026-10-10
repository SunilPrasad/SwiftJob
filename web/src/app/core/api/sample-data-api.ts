import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class SampleDataApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/settings/sample-data';

  count(): Observable<{ sampleEmails: number }> {
    return this.http.get<{ sampleEmails: number }>(this.baseUrl);
  }

  remove(): Observable<{ removedJobs: number }> {
    return this.http.delete<{ removedJobs: number }>(this.baseUrl);
  }
}
