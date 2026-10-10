import { HttpClient } from '@angular/common/http';
import { DestroyRef, Injectable, computed, inject, signal } from '@angular/core';
import { HubConnection, HubConnectionBuilder, LogLevel } from '@microsoft/signalr';
import { Observable, Subject } from 'rxjs';
import { SyncStatus } from '../models/sync';

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
    this.connection.onreconnected(() => this.connected.set(true));
    this.connection.onreconnecting(() => this.connected.set(false));
    this.connection
      .start()
      .then(() => this.connected.set(true))
      .catch(() => this.refresh());
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
