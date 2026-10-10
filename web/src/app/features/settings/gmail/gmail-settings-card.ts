import { Clipboard } from '@angular/cdk/clipboard';
import { DatePipe } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  inject,
  signal,
  untracked,
} from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSelectModule } from '@angular/material/select';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatTooltipModule } from '@angular/material/tooltip';
import { ActivatedRoute, Router } from '@angular/router';
import { Observable, finalize } from 'rxjs';
import { GmailSettingsApi } from '../../../core/api/gmail-settings-api';
import { GmailSettings, GmailTestResult } from '../../../core/models/gmail';

@Component({
  selector: 'app-gmail-settings-card',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    DatePipe,
    ReactiveFormsModule,
    MatButtonModule,
    MatCardModule,
    MatExpansionModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressBarModule,
    MatSelectModule,
    MatSlideToggleModule,
    MatTooltipModule,
  ],
  templateUrl: './gmail-settings-card.html',
  styleUrl: './gmail-settings-card.scss',
})
export class GmailSettingsCard {
  private readonly api = inject(GmailSettingsApi);
  private readonly snackBar = inject(MatSnackBar);
  private readonly clipboard = inject(Clipboard);
  private readonly router = inject(Router);

  protected readonly settings = rxResource({ stream: () => this.api.get() });
  protected readonly intervalOptions = [
    { minutes: 15, label: '15 minutes' },
    { minutes: 30, label: '30 minutes' },
    { minutes: 60, label: 'hour' },
    { minutes: 180, label: '3 hours' },
    { minutes: 720, label: '12 hours' },
    { minutes: 1440, label: 'day' },
  ];
  protected readonly busy = signal(false);
  protected readonly testResult = signal<GmailTestResult | null>(null);
  protected readonly editingCredentials = signal(false);

  protected readonly credentialsForm = new FormGroup({
    clientId: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.minLength(10)],
    }),
    clientSecret: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.minLength(10)],
    }),
  });
  protected readonly searchQuery = new FormControl('', {
    nonNullable: true,
    validators: [Validators.required, Validators.maxLength(1000)],
  });

  protected readonly state = computed<'not-configured' | 'ready' | 'connected'>(() => {
    const s = this.settings.value();
    return s?.isConnected ? 'connected' : s?.hasClientCredentials ? 'ready' : 'not-configured';
  });
  protected readonly showCredentialsForm = computed(
    () =>
      this.state() === 'not-configured' || (this.state() === 'ready' && this.editingCredentials()),
  );

  constructor() {
    this.handleOAuthRedirect();

    // Show the saved query whenever settings load or are saved.
    effect(() => {
      const saved = this.settings.value()?.searchQuery;
      if (saved !== undefined) {
        untracked(() => this.searchQuery.reset(saved));
      }
    });
  }

  protected copyRedirectUri(uri: string): void {
    this.clipboard.copy(uri);
    this.snackBar.open('Redirect URI copied', undefined, { duration: 2000 });
  }

  protected editCredentials(settings: GmailSettings): void {
    this.credentialsForm.reset({ clientId: settings.clientId ?? '', clientSecret: '' });
    this.editingCredentials.set(true);
  }

  protected saveCredentials(): void {
    if (this.credentialsForm.invalid) {
      this.credentialsForm.markAllAsTouched();
      return;
    }
    const { clientId, clientSecret } = this.credentialsForm.getRawValue();
    this.run(this.api.saveCredentials(clientId, clientSecret), (s) => {
      this.credentialsForm.reset();
      this.editingCredentials.set(false);
      this.settings.set(s);
      this.snackBar.open('Google OAuth client saved', undefined, { duration: 3000 });
    });
  }

  protected removeCredentials(): void {
    if (confirm('Remove the saved Google OAuth Client ID and Secret?')) {
      this.run(this.api.removeCredentials(), (s) => {
        this.editingCredentials.set(false);
        this.settings.set(s);
      });
    }
  }

  /** Sends the browser to Google's consent screen; Google redirects back to the API, which returns here. */
  protected connect(): void {
    this.run(this.api.connect(), ({ authorizationUrl }) => {
      if (authorizationUrl.startsWith('https://accounts.google.com/')) {
        window.location.assign(authorizationUrl);
      }
    });
  }

  protected disconnect(): void {
    if (
      confirm(
        'Disconnect Gmail? SwiftJob will stop reading your emails and its access will be revoked.',
      )
    ) {
      this.run(this.api.disconnect(), (s) => {
        this.testResult.set(null);
        this.settings.set(s);
        this.snackBar.open('Gmail disconnected', undefined, { duration: 3000 });
      });
    }
  }

  protected saveQuery(): void {
    if (this.searchQuery.invalid) {
      return;
    }
    this.run(this.api.updateSearchQuery(this.searchQuery.value), (s) => {
      this.settings.set(s);
      this.testResult.set(null);
      this.snackBar.open('Email filter saved', undefined, { duration: 3000 });
    });
  }

  protected resetQuery(settings: GmailSettings): void {
    this.searchQuery.setValue(settings.defaultSearchQuery);
    this.searchQuery.markAsDirty();
  }

  protected saveSchedule(autoSyncEnabled: boolean, minutes: number): void {
    this.run(this.api.updateSyncSchedule(autoSyncEnabled, minutes), (s) => this.settings.set(s));
  }

  /** Re-reads settings (e.g. after a sync finished elsewhere). */
  reload(): void {
    this.settings.reload();
  }

  protected test(): void {
    this.testResult.set(null);
    this.run(this.api.test(), (result) => this.testResult.set(result));
  }

  private run<T>(request: Observable<T>, onSuccess: (value: T) => void): void {
    this.busy.set(true);
    // Errors are shown by apiErrorInterceptor; refresh to reflect any server-side change (e.g. revoked access).
    request.pipe(finalize(() => this.busy.set(false))).subscribe({
      next: onSuccess,
      error: () => this.settings.reload(),
    });
  }

  /** Shows the outcome of the Google sign-in redirect (?gmail=connected|error&message=...) once, then cleans the URL. */
  private handleOAuthRedirect(): void {
    const params = inject(ActivatedRoute).snapshot.queryParamMap;
    const result = params.get('gmail');
    if (!result) {
      return;
    }
    const message =
      result === 'connected'
        ? 'Gmail connected with read-only access'
        : `Gmail was not connected: ${params.get('message') ?? 'unknown error'}`;
    this.snackBar.open(message, 'Dismiss', { duration: result === 'connected' ? 4000 : 10000 });
    void this.router.navigate([], { queryParams: {}, replaceUrl: true });
  }
}
