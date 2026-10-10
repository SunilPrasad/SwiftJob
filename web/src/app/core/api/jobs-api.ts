import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import {
  JobDetail,
  JobFilter,
  JobListItem,
  JobListQuery,
  JobStatus,
  JobStatusCounts,
  PagedResult,
} from '../models/job';

/** Thin typed client for the jobs endpoints. Holds no state. */
@Injectable({ providedIn: 'root' })
export class JobsApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/jobs';

  getJobs(query: JobListQuery): Observable<PagedResult<JobListItem>> {
    let params = toFilterParams(query)
      .set('sortBy', query.sortBy)
      .set('sortDirection', query.sortDirection)
      .set('page', query.page)
      .set('pageSize', query.pageSize);
    if (query.status) {
      params = params.set('status', query.status);
    }
    return this.http.get<PagedResult<JobListItem>>(this.baseUrl, { params });
  }

  getCounts(filter: JobFilter): Observable<JobStatusCounts> {
    return this.http.get<JobStatusCounts>(`${this.baseUrl}/counts`, {
      params: toFilterParams(filter),
    });
  }

  getSources(): Observable<string[]> {
    return this.http.get<string[]>(`${this.baseUrl}/sources`);
  }

  getJob(id: number): Observable<JobDetail> {
    return this.http.get<JobDetail>(`${this.baseUrl}/${id}`);
  }

  updateStatus(id: number, status: JobStatus): Observable<JobDetail> {
    return this.http.put<JobDetail>(`${this.baseUrl}/${id}/status`, { status });
  }
}

function toFilterParams(filter: JobFilter): HttpParams {
  let params = new HttpParams();
  const search = filter.search.trim();
  if (search) {
    params = params.set('search', search);
  }
  if (filter.source) {
    params = params.set('source', filter.source);
  }
  if (filter.receivedFrom) {
    params = params.set('receivedFrom', filter.receivedFrom);
  }
  if (filter.receivedBefore) {
    params = params.set('receivedBefore', filter.receivedBefore);
  }
  return params;
}
