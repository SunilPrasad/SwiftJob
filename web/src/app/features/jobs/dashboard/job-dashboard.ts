import { BreakpointObserver } from '@angular/cdk/layout';
import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  ElementRef,
  afterNextRender,
  computed,
  inject,
  signal,
} from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Router } from '@angular/router';
import { finalize, map } from 'rxjs';
import { JOB_STATUS_LABELS, JobListItem, JobStatus } from '../../../core/models/job';
import { SyncStatusService } from '../../../core/realtime/sync-status';
import { ActivityLog } from '../../activity/activity-log';
import { SplitResizer } from '../../../shared/split-resizer';
import { JobsStore } from '../data/jobs-store';
import { JobDetailsPanel } from '../details/job-details-panel';
import { JobFilters } from '../filters/job-filters';
import { JobStatusChange } from '../shared/job-status-actions';
import { JobTable } from '../table/job-table';
import {
  DETAILS_MIN_WIDTH,
  LIST_MIN_WIDTH,
  defaultDetailsWidth,
  loadDetailsWidth,
  saveDetailsWidth,
} from './details-width';

@Component({
  selector: 'app-job-dashboard',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [JobsStore],
  imports: [
    MatButtonModule,
    MatIconModule,
    MatProgressBarModule,
    ActivityLog,
    JobDetailsPanel,
    JobFilters,
    JobTable,
    SplitResizer,
  ],
  templateUrl: './job-dashboard.html',
  styleUrl: './job-dashboard.scss',
})
export class JobDashboard {
  protected readonly store = inject(JobsStore);
  private readonly snackBar = inject(MatSnackBar);
  private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);
  private readonly router = inject(Router);
  protected readonly sync = inject(SyncStatusService);
  protected readonly syncRequested = signal(false);
  protected readonly showActivity = signal(readActivityPreference());

  /** On phones there is no room to split: the details replace the list. */
  protected readonly isHandset = toSignal(
    inject(BreakpointObserver)
      .observe('(max-width: 720px)')
      .pipe(map((s) => s.matches)),
    { initialValue: false },
  );

  protected readonly panelOpen = computed(() => this.store.selectedJobId() !== null);
  protected readonly page = computed(() => this.store.jobs.value());

  protected readonly minDetailsWidth = DETAILS_MIN_WIDTH;
  private readonly containerWidth = signal(window.innerWidth);
  private readonly preferredWidth = signal<number | null>(loadDetailsWidth());

  protected readonly maxDetailsWidth = computed(() =>
    Math.max(DETAILS_MIN_WIDTH, this.containerWidth() - LIST_MIN_WIDTH),
  );
  /** Width actually used: the user's choice (or the default), kept within the space available. */
  protected readonly detailsWidth = computed(() => {
    const preferred = this.preferredWidth() ?? defaultDetailsWidth(this.containerWidth());
    return Math.min(this.maxDetailsWidth(), Math.max(DETAILS_MIN_WIDTH, preferred));
  });

  constructor() {
    this.sync.connect();
    this.sync.completed$.pipe(takeUntilDestroyed()).subscribe((result) => {
      this.store.refresh();
      const failed = result.state === 'Failed';
      const ref = this.snackBar.open(
        failed ? `Sync failed: ${result.message}` : result.message,
        failed ? 'Settings' : 'OK',
        { duration: failed ? 10000 : 6000 },
      );
      if (failed) {
        ref.onAction().subscribe(() => void this.router.navigate(['/settings']));
      }
    });

    const observer = new ResizeObserver(([entry]) =>
      this.containerWidth.set(entry.contentRect.width),
    );
    afterNextRender(() => observer.observe(this.host.nativeElement));
    inject(DestroyRef).onDestroy(() => observer.disconnect());
  }

  protected openJob(job: JobListItem): void {
    this.store.select(job.id);
  }

  protected closePanel(): void {
    this.store.select(null);
  }

  protected resizeDetails(width: number): void {
    this.preferredWidth.set(width);
    saveDetailsWidth(width);
  }

  protected resetDetailsWidth(): void {
    this.preferredWidth.set(null);
    saveDetailsWidth(null);
  }

  protected changeStatusFilter(status: JobStatus | null): void {
    this.store.setStatusFilter(status);
  }

  protected changeJobStatus(change: JobStatusChange): void {
    // Failures are already reported to the user by apiErrorInterceptor.
    this.store.updateStatus(change.jobId, change.status).subscribe({
      next: () => {
        const ref = this.snackBar.open(
          `“${change.title}” marked as ${JOB_STATUS_LABELS[change.status].toLowerCase()}`,
          'Undo',
          { duration: 5000 },
        );
        ref
          .onAction()
          .subscribe(() =>
            this.store
              .updateStatus(change.jobId, change.previousStatus)
              .subscribe({ error: () => undefined }),
          );
      },
      error: () => undefined,
    });
  }

  protected toggleActivity(): void {
    this.showActivity.update((v) => !v);
    try {
      localStorage.setItem(ACTIVITY_KEY, String(this.showActivity()));
    } catch {
      // Preference simply isn't remembered.
    }
  }

  protected syncJobs(): void {
    this.syncRequested.set(true);
    this.showActivity.set(true); // show what happens
    // Failures (e.g. Gmail not connected) are reported by apiErrorInterceptor.
    this.sync
      .start()
      .pipe(finalize(() => this.syncRequested.set(false)))
      .subscribe({ error: () => undefined });
  }

  protected refresh(): void {
    this.store.refresh();
  }
}

const ACTIVITY_KEY = 'swiftjob.showActivity';

function readActivityPreference(): boolean {
  try {
    return localStorage.getItem(ACTIVITY_KEY) === 'true';
  } catch {
    return false;
  }
}
