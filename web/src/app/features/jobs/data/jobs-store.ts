import { Injectable, computed, inject, signal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { Observable, tap } from 'rxjs';
import { JobsApi } from '../../../core/api/jobs-api';
import {
  JobDetail,
  JobFilter,
  JobListQuery,
  JobSortField,
  JobStatus,
  SortDirection,
} from '../../../core/models/job';

export const DEFAULT_JOB_QUERY: JobListQuery = {
  status: 'New',
  search: '',
  source: null,
  receivedFrom: null,
  receivedBefore: null,
  sortBy: 'ReceivedAt',
  sortDirection: 'Desc',
  page: 1,
  pageSize: 25,
};

/**
 * State for the job dashboard: the current query, the resulting page of jobs, status counts,
 * and the job selected in the details panel. Provided per dashboard instance.
 */
@Injectable()
export class JobsStore {
  private readonly api = inject(JobsApi);

  readonly query = signal<JobListQuery>(DEFAULT_JOB_QUERY);
  readonly selectedJobId = signal<number | null>(null);

  /** Filters without status/sort/paging, so the counts show every status for the current search. */
  private readonly filter = computed<JobFilter>(
    () => {
      const { search, source, receivedFrom, receivedBefore } = this.query();
      return { search, source, receivedFrom, receivedBefore };
    },
    { equal: (a, b) => JSON.stringify(a) === JSON.stringify(b) },
  );

  readonly jobs = rxResource({
    params: () => this.query(),
    stream: ({ params }) => this.api.getJobs(params),
  });

  readonly counts = rxResource({
    params: () => this.filter(),
    stream: ({ params }) => this.api.getCounts(params),
  });

  readonly sources = rxResource({
    stream: () => this.api.getSources(),
    defaultValue: [],
  });

  readonly selectedJob = rxResource({
    params: () => this.selectedJobId() ?? undefined,
    stream: ({ params }) => this.api.getJob(params),
  });

  readonly hasActiveFilters = computed(() => {
    const q = this.query();
    return !!(q.search.trim() || q.source || q.receivedFrom || q.receivedBefore);
  });

  setStatusFilter(status: JobStatus | null): void {
    this.patchQuery({ status });
  }

  setSearch(search: string): void {
    this.patchQuery({ search });
  }

  setSource(source: string | null): void {
    this.patchQuery({ source });
  }

  setDateRange(receivedFrom: string | null, receivedBefore: string | null): void {
    this.patchQuery({ receivedFrom, receivedBefore });
  }

  setSort(sortBy: JobSortField, sortDirection: SortDirection): void {
    this.patchQuery({ sortBy, sortDirection });
  }

  /** @param page 1-based page number. */
  setPage(page: number, pageSize: number): void {
    this.query.update((q) => ({ ...q, page: pageSize === q.pageSize ? page : 1, pageSize }));
  }

  clearFilters(): void {
    this.patchQuery({ search: '', source: null, receivedFrom: null, receivedBefore: null });
  }

  select(jobId: number | null): void {
    this.selectedJobId.set(jobId);
  }

  /** Re-fetches the list, counts, sources and the open job. */
  refresh(): void {
    this.jobs.reload();
    this.counts.reload();
    this.sources.reload();
    this.selectedJob.reload();
  }

  /**
   * Persists a status change and refreshes affected views. The job stays in the database;
   * it simply leaves the current status view.
   */
  updateStatus(jobId: number, status: JobStatus): Observable<JobDetail> {
    return this.api.updateStatus(jobId, status).pipe(
      tap((updated) => {
        if (this.selectedJobId() === updated.id) {
          this.selectedJob.set(updated);
        }
        this.jobs.reload();
        this.counts.reload();
      }),
    );
  }

  /** Any filter change resets to the first page. */
  private patchQuery(patch: Partial<JobListQuery>): void {
    this.query.update((q) => ({ ...q, ...patch, page: 1 }));
  }
}
