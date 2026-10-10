import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { SyncLogEntry } from '../../core/models/sync';
import { SyncStatusService } from '../../core/realtime/sync-status';
import { ActivityLog } from './activity-log';

const entry = (id: number, level: SyncLogEntry['level'], message: string): SyncLogEntry => ({
  id,
  timestamp: '2026-10-10T10:00:00Z',
  level,
  message,
});

describe('ActivityLog', () => {
  let fixture: ComponentFixture<ActivityLog>;
  let http: HttpTestingController;
  let service: SyncStatusService;

  beforeEach(async () => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    http = TestBed.inject(HttpTestingController);
    service = TestBed.inject(SyncStatusService);
    // Avoid opening a real SignalR connection in unit tests.
    (service as unknown as { connection: object }).connection = { stop: () => Promise.resolve() };
    fixture = TestBed.createComponent(ActivityLog);
    await fixture.whenStable();
  });

  const append = (entries: SyncLogEntry[]) =>
    (service as unknown as { appendLog(e: SyncLogEntry[]): void }).appendLog(entries);

  it('shows an empty state, then lines as plain text in order without duplicates', async () => {
    expect(fixture.nativeElement.textContent).toContain('No activity yet');

    append([entry(2, 'Info', 'Found 3 emails'), entry(1, 'Info', 'Sync started')]);
    append([entry(2, 'Info', 'Found 3 emails'), entry(3, 'Error', '<b>AI failed</b>')]);
    await fixture.whenStable();

    const lines = Array.from(fixture.nativeElement.querySelectorAll('.line')).map((l) =>
      (l as HTMLElement).querySelector('.message')!.textContent!.trim(),
    );
    expect(lines).toEqual(['Sync started', 'Found 3 emails', '<b>AI failed</b>']);
    expect(fixture.nativeElement.querySelector('b')).toBeNull();
  });

  it('can show problems only', async () => {
    append([entry(1, 'Info', 'ok'), entry(2, 'Warning', 'careful'), entry(3, 'Error', 'bad')]);
    await fixture.whenStable();

    (
      fixture.nativeElement.querySelector(
        'button[aria-label="Toggle problems only"]',
      ) as HTMLButtonElement
    ).click();
    await fixture.whenStable();

    expect(fixture.nativeElement.querySelectorAll('.line').length).toBe(2);
  });

  it('loads recent history from the API', () => {
    service.loadLog();
    http.expectOne('/api/sync/log').flush([entry(7, 'Info', 'from server')]);

    expect(service.log().map((e) => e.message)).toEqual(['from server']);
  });
});
