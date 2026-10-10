import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { Observable, finalize } from 'rxjs';
import { BrowserApi } from '../../core/api/browser-api';
import { BrowserStatus } from '../../core/models/browser';
import { SyncStatusService } from '../../core/realtime/sync-status';
import { ActivityLog } from '../activity/activity-log';

/**
 * Prototype page: try the SwiftJob browser and the AI form filler on a local sample form,
 * including Take control / Resume. Nothing is submitted anywhere.
 */
@Component({
  selector: 'app-browser-lab',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatButtonModule, MatCardModule, MatIconModule, ActivityLog],
  templateUrl: './browser-lab.html',
  styleUrl: './browser-lab.scss',
})
export class BrowserLab {
  private readonly api = inject(BrowserApi);
  protected readonly realtime = inject(SyncStatusService);
  protected readonly busy = signal(false);

  protected readonly status = computed(() => this.realtime.browser());
  protected readonly aiWorking = computed(() => this.status()?.controller === 'Agent');
  protected readonly isOpen = computed(() => this.status()?.isOpen ?? false);

  constructor() {
    this.realtime.connect();
  }

  protected openSample(): void {
    this.run(this.api.openDemoForm());
  }

  protected aiFill(): void {
    this.run(this.api.startDemoFill());
  }

  protected takeControl(): void {
    this.run(this.api.takeControl());
  }

  protected resume(): void {
    this.run(this.api.resume());
  }

  protected stop(): void {
    this.run(this.api.stop());
  }

  protected arrange(): void {
    this.run(this.api.arrange());
  }

  protected close(): void {
    this.run(this.api.close());
  }

  private run(request: Observable<BrowserStatus>): void {
    this.busy.set(true);
    request.pipe(finalize(() => this.busy.set(false))).subscribe({
      next: (s) => this.realtime.browser.set(s),
      error: () => undefined, // shown by apiErrorInterceptor
    });
  }
}
