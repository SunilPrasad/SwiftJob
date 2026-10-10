import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { DEFAULT_JOB_QUERY } from '../../features/jobs/data/jobs-store';
import { JobsApi } from './jobs-api';

describe('JobsApi', () => {
  let api: JobsApi;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    api = TestBed.inject(JobsApi);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('sends only the filters that are set', () => {
    api
      .getJobs({ ...DEFAULT_JOB_QUERY, search: '  angular ', source: 'Reed', page: 2 })
      .subscribe();

    const req = http.expectOne((r) => r.url === '/api/jobs');
    expect(req.request.params.get('status')).toBe('New');
    expect(req.request.params.get('search')).toBe('angular');
    expect(req.request.params.get('source')).toBe('Reed');
    expect(req.request.params.get('page')).toBe('2');
    expect(req.request.params.get('pageSize')).toBe('25');
    expect(req.request.params.get('sortBy')).toBe('ReceivedAt');
    expect(req.request.params.get('sortDirection')).toBe('Desc');
    expect(req.request.params.has('receivedFrom')).toBe(false);
    req.flush({ items: [], totalCount: 0, page: 2, pageSize: 25 });
  });

  it('omits status to request all statuses', () => {
    api.getJobs({ ...DEFAULT_JOB_QUERY, status: null }).subscribe();

    const req = http.expectOne((r) => r.url === '/api/jobs');
    expect(req.request.params.has('status')).toBe(false);
    req.flush({ items: [], totalCount: 0, page: 1, pageSize: 25 });
  });

  it('updates status with PUT', () => {
    api.updateStatus(7, 'NotRelevant').subscribe();

    const req = http.expectOne('/api/jobs/7/status');
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual({ status: 'NotRelevant' });
    req.flush({});
  });
});
