import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { JobEmailDetail } from '../models/job';

@Injectable({ providedIn: 'root' })
export class EmailsApi {
  private readonly http = inject(HttpClient);

  getEmail(id: number): Observable<JobEmailDetail> {
    return this.http.get<JobEmailDetail>(`/api/emails/${id}`);
  }
}
