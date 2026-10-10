import { DatePipe } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  afterRenderEffect,
  computed,
  inject,
  output,
  signal,
  viewChild,
} from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { SyncStatusService } from '../../core/realtime/sync-status';

/** Live view of what the background sync is doing. Messages are rendered as plain text. */
@Component({
  selector: 'app-activity-log',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DatePipe, MatButtonModule, MatIconModule, MatTooltipModule],
  template: `
    <div class="header">
      <mat-icon>receipt_long</mat-icon>
      <span class="title">Activity</span>
      <span class="live" [class.on]="sync.connected()">{{
        sync.connected() ? 'live' : 'offline'
      }}</span>
      <span class="spacer"></span>
      <button
        mat-icon-button
        matTooltip="Show only warnings and errors"
        aria-label="Toggle problems only"
        [class.active]="problemsOnly()"
        (click)="problemsOnly.set(!problemsOnly())"
      >
        <mat-icon>filter_list</mat-icon>
      </button>
      <button
        mat-icon-button
        matTooltip="Close"
        aria-label="Close activity log"
        (click)="closed.emit()"
      >
        <mat-icon>close</mat-icon>
      </button>
    </div>
    <div #scroller class="lines" role="log" aria-live="polite" (scroll)="onScroll()">
      @for (entry of entries(); track entry.id) {
        <div class="line" [class]="'line ' + entry.level">
          <span class="time">{{ entry.timestamp | date: 'HH:mm:ss' }}</span>
          <mat-icon class="level">{{
            entry.level === 'Error'
              ? 'error'
              : entry.level === 'Warning'
                ? 'warning'
                : 'chevron_right'
          }}</mat-icon>
          <span class="message">{{ entry.message }}</span>
        </div>
      } @empty {
        <div class="empty">
          No activity yet. Click “Sync jobs” or wait for the next automatic sync.
        </div>
      }
    </div>
  `,
  styles: `
    :host {
      display: flex;
      flex-direction: column;
      border: 1px solid var(--mat-sys-outline-variant);
      border-radius: 12px;
      overflow: hidden;
      background: var(--mat-sys-surface-container-lowest);
    }
    .header {
      display: flex;
      align-items: center;
      gap: 8px;
      padding: 2px 4px 2px 12px;
      background: var(--mat-sys-surface-container);
    }
    .title {
      font: var(--mat-sys-title-small);
    }
    .live {
      font: var(--mat-sys-label-small);
      padding: 0 8px;
      border-radius: 8px;
      background: var(--mat-sys-surface-container-highest);
      &.on {
        background: #d7f5dd;
        color: #0d5c25;
      }
    }
    .spacer {
      flex: 1;
    }
    .active {
      color: var(--mat-sys-primary);
    }
    .lines {
      height: 220px;
      overflow-y: auto;
      padding: 6px 0;
      font-family: ui-monospace, Menlo, Consolas, monospace;
      font-size: 12px;
      line-height: 1.5;
    }
    .line {
      display: flex;
      gap: 8px;
      padding: 0 12px;
      &.Warning {
        color: #8a5a00;
      }
      &.Error {
        color: var(--mat-sys-error);
      }
    }
    .time {
      flex: 0 0 auto;
      color: var(--mat-sys-on-surface-variant);
    }
    .level {
      flex: 0 0 auto;
      font-size: 16px;
      width: 16px;
      height: 16px;
      margin-top: 1px;
    }
    .message {
      white-space: pre-wrap;
      word-break: break-word;
    }
    .empty {
      padding: 12px;
      color: var(--mat-sys-on-surface-variant);
      font-family: var(--mat-sys-body-small-font);
    }
  `,
})
export class ActivityLog {
  protected readonly sync = inject(SyncStatusService);
  readonly closed = output<void>();

  protected readonly problemsOnly = signal(false);
  protected readonly entries = computed(() =>
    this.problemsOnly() ? this.sync.log().filter((e) => e.level !== 'Info') : this.sync.log(),
  );

  private readonly scroller = viewChild.required<ElementRef<HTMLElement>>('scroller');
  /** Follow new lines unless the user has scrolled up to read older ones. */
  private followTail = true;

  constructor() {
    this.sync.connect();
    afterRenderEffect(() => {
      this.entries();
      const el = this.scroller().nativeElement;
      if (this.followTail) {
        el.scrollTop = el.scrollHeight;
      }
    });
  }

  protected onScroll(): void {
    const el = this.scroller().nativeElement;
    this.followTail = el.scrollHeight - el.scrollTop - el.clientHeight < 24;
  }
}
