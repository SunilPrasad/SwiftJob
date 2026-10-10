import { HttpClient } from '@angular/common/http';
import { DestroyRef, Injectable, computed, inject, signal } from '@angular/core';
import { HubConnection, HubConnectionBuilder, LogLevel } from '@microsoft/signalr';
import { Observable, Subject } from 'rxjs';
import { SyncLogEntry, SyncStatus } from '../models/sync';

const MAX_LOG_ENTRIES = 500;

/**
 * Live Gmail synchronisation status, pushed by the backend over SignalR (/hubs/sync).
 * Falls back to the REST status endpoint while the real-time connection is unavailable.
 */
@Injectable({ providedIn: 'root' })
export class SyncStatusService {
  private readonly http = inject(HttpClient);
  private readonly completedSubject = new Subject<SyncStatus>();
  private connection: HubConnection | null = null;
  /** Set when this browser started a sync, so a very fast run is still reported as finished. */
  private requestedAt: number | null = null;

  readonly status = signal<SyncStatus | null>(null);
  readonly isRunning = computed(() => this.status()?.state === 'Running');
  readonly connected = signal(false);

  /** Background activity log, oldest first (kept by the API in memory, newest 500 lines). */
  readonly log = signal<SyncLogEntry[]>([]);

  /** Emits once each time a synchronisation finishes (successfully or not). */
  readonly completed$: Observable<SyncStatus> = this.completedSubject.asObservable();

  constructor() {
    inject(DestroyRef).onDestroy(() => void this.connection?.stop());
  }

  /** Opens the real-time connection (idempotent). */
  connect(): void {
    if (this.connection) {
      return;
    }
    this.connection = new HubConnectionBuilder()
      .withUrl('/hubs/sync')
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build();
    this.connection.on('syncStatus', (status: SyncStatus) => this.update(status));
    this.connection.on('syncLog', (entry: SyncLogEntry) => this.appendLog([entry]));
    this.connection.onreconnected(() => {
      this.connected.set(true);
      this.loadLog(); // catch up on lines missed while disconnected
    });
    this.connection.onreconnecting(() => this.connected.set(false));
    this.connection
      .start()
      .then(() => this.connected.set(true))
      .catch(() => this.refresh());
    this.loadLog();
  }

  /** Fetches the recent activity log (merged with lines already received live). */
  loadLog(): void {
    this.http.get<SyncLogEntry[]>('/api/sync/log').subscribe({
      next: (entries) => this.appendLog(entries),
      error: () => undefined,
    });
  }

  private appendLog(entries: SyncLogEntry[]): void {
    this.log.update((current) => {
      const byId = new Map(current.map((e) => [e.id, e]));
      for (const entry of entries) {
        byId.set(entry.id, entry);
      }
      return [...byId.values()].sort((a, b) => a.id - b.id).slice(-MAX_LOG_ENTRIES);
    });
  }

  /** Asks the backend to start a synchronisation. Progress then arrives over SignalR. */
  start(): Observable<SyncStatus> {
    this.requestedAt = Date.now();
    return this.http.post<SyncStatus>('/api/sync', null);
  }

  refresh(): void {
    this.http.get<SyncStatus>('/api/sync/status').subscribe({
      next: (s) => this.update(s),
      error: () => undefined,
    });
  }

  private update(next: SyncStatus): void {
    const previous = this.status();
    this.status.set(next);
    const finished = next.state === 'Completed' || next.state === 'Failed';
    const finishedAfterRequest =
      this.requestedAt !== null &&
      next.completedAt !== null &&
      Date.parse(next.completedAt) >= this.requestedAt - 1000;
    if (finished && (previous?.state === 'Running' || finishedAfterRequest)) {
      this.requestedAt = null;
      this.completedSubject.next(next);
    }
  }
}
