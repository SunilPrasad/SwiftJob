import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { SyncStatus } from '../models/sync';
import { SyncStatusService } from './sync-status';

const status = (state: SyncStatus['state'], completedAt: string | null = null): SyncStatus => ({
  state,
  message: state,
  processed: 0,
  total: 0,
  newJobs: 0,
  startedAt: null,
  completedAt,
});

describe('SyncStatusService', () => {
  let service: SyncStatusService;
  let http: HttpTestingController;
  let completed: SyncStatus[];

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(SyncStatusService);
    http = TestBed.inject(HttpTestingController);
    completed = [];
    service.completed$.subscribe((s) => completed.push(s));
  });

  const push = (s: SyncStatus) => (service as unknown as { update(s: SyncStatus): void }).update(s);

  it('reports completion after a running sync', () => {
    push(status('Running'));
    expect(service.isRunning()).toBe(true);

    push(status('Completed', new Date().toISOString()));

    expect(service.isRunning()).toBe(false);
    expect(completed.length).toBe(1);
  });

  it('does not report an old completed status on connect', () => {
    push(status('Completed', '2020-01-01T00:00:00Z'));

    expect(completed.length).toBe(0);
  });

  it('reports a fast sync that finished before progress was seen', () => {
    service.start().subscribe();
    http.expectOne('/api/sync').flush(status('Idle'));

    push(status('Completed', new Date().toISOString()));

    expect(completed.length).toBe(1);
  });
});
