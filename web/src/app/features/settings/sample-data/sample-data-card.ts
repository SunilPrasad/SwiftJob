import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatSnackBar } from '@angular/material/snack-bar';
import { finalize } from 'rxjs';
import { SampleDataApi } from '../../../core/api/sample-data-api';

/** Only shown while development sample jobs exist. */
@Component({
  selector: 'app-sample-data-card',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatButtonModule, MatCardModule, MatIconModule],
  template: `
    @if ((info.value()?.sampleEmails ?? 0) > 0) {
      <mat-card appearance="outlined">
        <mat-card-header>
          <mat-icon mat-card-avatar class="avatar">science</mat-icon>
          <mat-card-title>Sample data</mat-card-title>
          <mat-card-subtitle
            >The dashboard still contains demo jobs created for development.</mat-card-subtitle
          >
        </mat-card-header>
        <mat-card-content>
          <p>
            Remove them now that real jobs come from Gmail. Jobs from your own emails are not
            affected.
          </p>
          <button mat-stroked-button (click)="remove()" [disabled]="busy()">
            <mat-icon>delete_sweep</mat-icon> Remove sample jobs
          </button>
        </mat-card-content>
      </mat-card>
    }
  `,
  styles: `
    .avatar {
      color: var(--mat-sys-tertiary);
      font-size: 32px;
      width: 40px;
      height: 40px;
      display: flex;
      align-items: center;
      justify-content: center;
    }
  `,
})
export class SampleDataCard {
  private readonly api = inject(SampleDataApi);
  private readonly snackBar = inject(MatSnackBar);

  protected readonly info = rxResource({ stream: () => this.api.count() });
  protected readonly busy = signal(false);

  protected remove(): void {
    if (!confirm('Remove all sample jobs and emails?')) {
      return;
    }
    this.busy.set(true);
    this.api
      .remove()
      .pipe(finalize(() => this.busy.set(false)))
      .subscribe({
        next: ({ removedJobs }) => {
          this.snackBar.open(`Removed ${removedJobs} sample jobs`, undefined, { duration: 3000 });
          this.info.set({ sampleEmails: 0 });
        },
        error: () => undefined,
      });
  }
}
