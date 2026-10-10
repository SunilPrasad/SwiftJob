import { ChangeDetectionStrategy, Component, effect, inject, input, output } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { provideNativeDateAdapter } from '@angular/material/core';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { debounceTime, distinctUntilChanged } from 'rxjs';
import { JobStatus, JobStatusCounts } from '../../../core/models/job';
import { toUtcRange } from '../../../shared/date-range';
import { JobsStore } from '../data/jobs-store';

/** Value used by the "All" toggle; mat-button-toggle cannot represent null. */
const ALL = 'All';

@Component({
  selector: 'app-job-filters',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [provideNativeDateAdapter()],
  imports: [
    ReactiveFormsModule,
    MatButtonModule,
    MatButtonToggleModule,
    MatDatepickerModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatSelectModule,
  ],
  templateUrl: './job-filters.html',
  styleUrl: './job-filters.scss',
})
export class JobFilters {
  protected readonly store = inject(JobsStore);

  readonly counts = input<JobStatusCounts | undefined>();
  readonly sources = input<string[]>([]);
  readonly statusChange = output<JobStatus | null>();

  protected readonly ALL = ALL;

  protected readonly search = new FormControl('', { nonNullable: true });
  protected readonly source = new FormControl<string | null>(null);
  protected readonly dateRange = new FormGroup({
    start: new FormControl<Date | null>(null),
    end: new FormControl<Date | null>(null),
  });

  constructor() {
    this.search.valueChanges
      .pipe(debounceTime(300), distinctUntilChanged(), takeUntilDestroyed())
      .subscribe((value) => this.store.setSearch(value));

    this.source.valueChanges
      .pipe(takeUntilDestroyed())
      .subscribe((value) => this.store.setSource(value));

    this.dateRange.valueChanges.pipe(takeUntilDestroyed()).subscribe(({ start, end }) => {
      // Wait until the range is complete (or fully cleared) before querying.
      if ((start && end) || (!start && !end)) {
        const range = toUtcRange(start ?? null, end ?? null);
        this.store.setDateRange(range.receivedFrom, range.receivedBefore);
      }
    });

    // Keep the controls in sync when filters are cleared from elsewhere.
    effect(() => {
      if (!this.store.hasActiveFilters()) {
        this.search.setValue('', { emitEvent: false });
        this.source.setValue(null, { emitEvent: false });
        this.dateRange.setValue({ start: null, end: null }, { emitEvent: false });
      }
    });
  }

  protected get statusValue(): string {
    return this.store.query().status ?? ALL;
  }

  protected onStatusToggle(value: string): void {
    this.statusChange.emit(value === ALL ? null : (value as JobStatus));
  }

  protected clearSearch(): void {
    this.search.setValue('');
  }

  protected clear(): void {
    this.store.clearFilters();
  }
}
