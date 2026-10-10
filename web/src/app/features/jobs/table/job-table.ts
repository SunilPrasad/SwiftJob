import { BreakpointObserver } from '@angular/cdk/layout';
import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, input, output } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSortModule, Sort } from '@angular/material/sort';
import { MatTableModule } from '@angular/material/table';
import { MatTooltipModule } from '@angular/material/tooltip';
import { map } from 'rxjs';
import { JobListItem, JobSortField, JobStatus, SortDirection } from '../../../core/models/job';
import { JobStatusChange } from '../shared/job-status-actions';
import { JobStatusChip } from '../shared/job-status-chip';

const ALL_COLUMNS = [
  'receivedAt',
  'title',
  'company',
  'salary',
  'location',
  'source',
  'status',
  'actions',
];
const MEDIUM_COLUMNS = ['receivedAt', 'title', 'company', 'salary', 'status', 'actions'];
const SMALL_COLUMNS = ['title', 'status', 'actions'];

const SORT_FIELDS: Record<string, JobSortField> = {
  receivedAt: 'ReceivedAt',
  title: 'Title',
  company: 'Company',
};

@Component({
  selector: 'app-job-table',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    DatePipe,
    MatButtonModule,
    MatIconModule,
    MatMenuModule,
    MatPaginatorModule,
    MatProgressBarModule,
    MatSortModule,
    MatTableModule,
    MatTooltipModule,
    JobStatusChip,
  ],
  templateUrl: './job-table.html',
  styleUrl: './job-table.scss',
})
export class JobTable {
  readonly jobs = input<JobListItem[]>([]);
  readonly totalCount = input(0);
  /** 1-based. */
  readonly page = input(1);
  readonly pageSize = input(25);
  readonly sortBy = input<JobSortField>('ReceivedAt');
  readonly sortDirection = input<SortDirection>('Desc');
  readonly loading = input(false);
  readonly selectedJobId = input<number | null>(null);

  readonly rowClick = output<JobListItem>();
  readonly statusChange = output<JobStatusChange>();
  readonly pageChange = output<{ page: number; pageSize: number }>();
  readonly sortChange = output<{ sortBy: JobSortField; sortDirection: SortDirection }>();

  protected readonly pageSizeOptions = [10, 25, 50, 100];

  private readonly breakpoints = inject(BreakpointObserver);
  private readonly size = toSignal(
    this.breakpoints
      .observe(['(max-width: 720px)', '(max-width: 1100px)'])
      .pipe(
        map((state) =>
          state.breakpoints['(max-width: 720px)']
            ? 'small'
            : state.breakpoints['(max-width: 1100px)']
              ? 'medium'
              : 'large',
        ),
      ),
    { initialValue: 'large' },
  );

  protected readonly columns = computed(() =>
    this.size() === 'small'
      ? SMALL_COLUMNS
      : this.size() === 'medium'
        ? MEDIUM_COLUMNS
        : ALL_COLUMNS,
  );
  protected readonly compact = computed(() => this.size() === 'small');

  protected readonly activeSort = computed(
    () =>
      Object.entries(SORT_FIELDS).find(([, field]) => field === this.sortBy())?.[0] ?? 'receivedAt',
  );

  protected onSort(sort: Sort): void {
    // Clearing the sort falls back to the default: newest first.
    if (!sort.direction) {
      this.sortChange.emit({ sortBy: 'ReceivedAt', sortDirection: 'Desc' });
      return;
    }
    this.sortChange.emit({
      sortBy: SORT_FIELDS[sort.active] ?? 'ReceivedAt',
      sortDirection: sort.direction === 'asc' ? 'Asc' : 'Desc',
    });
  }

  protected onPage(event: PageEvent): void {
    this.pageChange.emit({ page: event.pageIndex + 1, pageSize: event.pageSize });
  }

  protected setStatus(event: Event, job: JobListItem, status: JobStatus): void {
    event.stopPropagation();
    this.statusChange.emit({ jobId: job.id, status, previousStatus: job.status, title: job.title });
  }

  protected stop(event: Event): void {
    event.stopPropagation();
  }
}
