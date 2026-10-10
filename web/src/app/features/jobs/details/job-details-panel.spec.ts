import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { JobDetail } from '../../../core/models/job';
import { JobStatusChange } from '../shared/job-status-actions';
import { JobDetailsPanel } from './job-details-panel';

const job: JobDetail = {
  id: 42,
  title: 'Senior .NET Developer',
  company: 'Fintech Ltd',
  salaryText: '£75,000 - £90,000',
  salaryMin: 75000,
  salaryMax: 90000,
  salaryCurrency: 'GBP',
  location: 'London',
  workMode: 'Hybrid',
  description: '<script>alert("x")</script> Build APIs.',
  requirements: '- C#',
  enrichedDescription: null,
  enrichedAt: null,
  applicationUrl: 'https://www.example.com/jobs/42',
  source: 'LinkedIn',
  sourceJobId: '123',
  receivedAt: '2026-10-01T10:00:00Z',
  createdAt: '2026-10-01T10:00:00Z',
  updatedAt: '2026-10-01T10:00:00Z',
  status: 'New',
  statusChangedAt: null,
  emails: [],
};

describe('JobDetailsPanel', () => {
  let fixture: ComponentFixture<JobDetailsPanel>;
  let element: HTMLElement;

  async function render(value: JobDetail) {
    fixture.componentRef.setInput('job', value);
    await fixture.whenStable();
  }

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    fixture = TestBed.createComponent(JobDetailsPanel);
    element = fixture.nativeElement;
  });

  it('renders job facts and treats the description as plain text', async () => {
    await render(job);

    expect(element.querySelector('.title')?.textContent).toContain('Senior .NET Developer');
    expect(element.textContent).toContain('£75,000 - £90,000');
    expect(element.textContent).toContain('Hybrid');
    expect(element.querySelector('script')).toBeNull();
    expect(element.querySelector('.text')?.textContent).toContain('<script>');
  });

  it('links only to safe http(s) application URLs', async () => {
    await render(job);
    const link = element.querySelector<HTMLAnchorElement>('a[target="_blank"]');
    expect(link?.getAttribute('href')).toBe('https://www.example.com/jobs/42');
    expect(link?.getAttribute('rel')).toContain('noopener');

    await render({ ...job, applicationUrl: 'javascript:alert(1)' });
    expect(element.querySelector('a[target="_blank"]')).toBeNull();
    expect(element.textContent).toContain('No valid application link');
  });

  it('emits status changes with the previous status for undo', async () => {
    await render(job);
    const emitted: JobStatusChange[] = [];
    fixture.componentInstance.statusChange.subscribe((c) => emitted.push(c));

    const button = Array.from(element.querySelectorAll('button')).find((b) =>
      b.textContent?.includes('Mark applied'),
    );
    button!.click();

    expect(emitted).toEqual([
      { jobId: 42, status: 'Applied', previousStatus: 'New', title: job.title },
    ]);
  });

  it('keeps Auto Apply disabled until the agent exists', async () => {
    await render(job);
    const autoApply = Array.from(element.querySelectorAll('button')).find((b) =>
      b.textContent?.includes('Auto apply'),
    );
    expect(autoApply?.disabled).toBe(true);
  });
});
