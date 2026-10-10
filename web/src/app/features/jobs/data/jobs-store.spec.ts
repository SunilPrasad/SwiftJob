import { ApplicationRef } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { JobsApi } from '../../../core/api/jobs-api';
import { JobDetail, JobListItem, JobListQuery, PagedResult } from '../../../core/models/job';
import { JobsStore } from './jobs-store';

const emptyPage: PagedResult<JobListItem> = { items: [], totalCount: 0, page: 1, pageSize: 25 };
const counts = { new: 1, applied: 0, notRelevant: 0, total: 1 };

function createApiMock() {
  return {
    getJobs: vi.fn((_: JobListQuery) => of(emptyPage)),
    getCounts: vi.fn(() => of(counts)),
    getSources: vi.fn(() => of(['LinkedIn'])),
    getJob: vi.fn((id: number) => of({ id, status: 'New' } as JobDetail)),
    updateStatus: vi.fn((id: number, status: string) => of({ id, status } as JobDetail)),
  };
}

describe('JobsStore', () => {
  let api: ReturnType<typeof createApiMock>;
  let store: JobsStore;

  const settle = () => TestBed.inject(ApplicationRef).whenStable();

  beforeEach(() => {
    api = createApiMock();
    TestBed.configureTestingModule({ providers: [JobsStore, { provide: JobsApi, useValue: api }] });
    store = TestBed.inject(JobsStore);
  });

  it('loads New jobs, newest first, by default', async () => {
    store.jobs.value(); // resources are lazy until read
    await settle();

    expect(api.getJobs).toHaveBeenCalledWith(
      expect.objectContaining({
        status: 'New',
        sortBy: 'ReceivedAt',
        sortDirection: 'Desc',
        page: 1,
      }),
    );
  });

  it('resets to the first page when a filter changes', () => {
    store.setPage(3, 25);
    expect(store.query().page).toBe(3);

    store.setSearch('angular');

    expect(store.query().page).toBe(1);
    expect(store.query().search).toBe('angular');
  });

  it('resets to the first page when the page size changes', () => {
    store.setPage(3, 25);
    store.setPage(3, 50);

    expect(store.query()).toEqual(expect.objectContaining({ page: 1, pageSize: 50 }));
  });

  it('does not re-query counts when only the status filter changes', async () => {
    store.counts.value();
    await settle();
    api.getCounts.mockClear();

    store.setStatusFilter('Applied');
    store.counts.value();
    await settle();

    expect(api.getCounts).not.toHaveBeenCalled();
  });

  it('reports active filters', () => {
    expect(store.hasActiveFilters()).toBe(false);
    store.setSource('Reed');
    expect(store.hasActiveFilters()).toBe(true);
    store.clearFilters();
    expect(store.hasActiveFilters()).toBe(false);
  });

  it('updates status, refreshes the list and counts, and updates the open job', async () => {
    store.select(5);
    store.selectedJob.value();
    store.jobs.value();
    store.counts.value();
    await settle();
    api.getJobs.mockClear();
    api.getCounts.mockClear();

    store.updateStatus(5, 'Applied').subscribe();
    await settle();

    expect(api.updateStatus).toHaveBeenCalledWith(5, 'Applied');
    expect(store.selectedJob.value()?.status).toBe('Applied');
    expect(api.getJobs).toHaveBeenCalled();
    expect(api.getCounts).toHaveBeenCalled();
  });
});
