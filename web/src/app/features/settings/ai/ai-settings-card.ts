import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  inject,
  signal,
  untracked,
} from '@angular/core';
import { rxResource, toSignal } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatAutocompleteModule } from '@angular/material/autocomplete';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSelectModule } from '@angular/material/select';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Observable, finalize, startWith } from 'rxjs';
import { AiSettingsApi } from '../../../core/api/ai-settings-api';
import { AiProvider, AiTestResult } from '../../../core/models/ai';

@Component({
  selector: 'app-ai-settings-card',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    ReactiveFormsModule,
    MatAutocompleteModule,
    MatButtonModule,
    MatCardModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressBarModule,
    MatSelectModule,
  ],
  templateUrl: './ai-settings-card.html',
  styleUrl: './ai-settings-card.scss',
})
export class AiSettingsCard {
  private readonly api = inject(AiSettingsApi);
  private readonly snackBar = inject(MatSnackBar);

  protected readonly settings = rxResource({ stream: () => this.api.get() });
  protected readonly busy = signal(false);
  protected readonly testResult = signal<AiTestResult | null>(null);

  protected readonly form = new FormGroup({
    provider: new FormControl<AiProvider>('Anthropic', { nonNullable: true }),
    model: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.maxLength(200)],
    }),
    endpoint: new FormControl('', { nonNullable: true }),
    apiKey: new FormControl('', { nonNullable: true }),
  });

  private readonly selectedProvider = toSignal(
    this.form.controls.provider.valueChanges.pipe(startWith(this.form.controls.provider.value)),
    { requireSync: true },
  );

  protected readonly providerInfo = computed(() =>
    this.settings.value()?.providers.find((p) => p.provider === this.selectedProvider()),
  );
  /** True when the stored key belongs to the selected provider (keys are dropped when the provider changes). */
  protected readonly keyStoredForSelection = computed(() => {
    const s = this.settings.value();
    return !!s?.hasApiKey && s.provider === this.selectedProvider();
  });

  constructor() {
    // Load saved values into the form whenever settings are (re)loaded.
    effect(() => {
      const s = this.settings.value();
      if (s) {
        untracked(() =>
          this.form.reset({
            provider: s.provider,
            model:
              s.model ??
              s.providers.find((p) => p.provider === s.provider)?.suggestedModels[0] ??
              '',
            endpoint: s.endpoint ?? '',
            apiKey: '',
          }),
        );
      }
    });

    // Sensible defaults when switching provider.
    this.form.controls.provider.valueChanges.subscribe((provider) => {
      const info = this.settings.value()?.providers.find((p) => p.provider === provider);
      const saved = this.settings.value();
      if (!info || saved?.provider === provider) {
        return;
      }
      this.form.patchValue({
        model: info.suggestedModels[0] ?? '',
        endpoint: info.defaultEndpoint ?? '',
        apiKey: '',
      });
    });
  }

  protected save(): void {
    const info = this.providerInfo();
    const { provider, model, endpoint, apiKey } = this.form.getRawValue();
    if (this.form.invalid || !info) {
      this.form.markAllAsTouched();
      return;
    }
    if (info.requiresApiKey && !apiKey && !this.keyStoredForSelection()) {
      this.form.controls.apiKey.setErrors({ required: true });
      this.form.controls.apiKey.markAsTouched();
      return;
    }
    this.run(
      this.api.save({
        provider,
        model: model.trim(),
        endpoint: info.requiresEndpoint ? endpoint.trim() : null,
        apiKey: apiKey.trim() || null,
        clearApiKey: false,
      }),
      (s) => {
        this.settings.set(s);
        this.testResult.set(null);
        this.snackBar.open('AI settings saved', undefined, { duration: 3000 });
      },
    );
  }

  protected removeKey(): void {
    const s = this.settings.value();
    if (!s?.model) {
      return;
    }
    this.run(
      this.api.save({
        provider: s.provider,
        model: s.model,
        endpoint: s.endpoint,
        apiKey: null,
        clearApiKey: true,
      }),
      (updated) => this.settings.set(updated),
    );
  }

  protected test(): void {
    this.testResult.set(null);
    this.run(this.api.test(), (result) => this.testResult.set(result));
  }

  private run<T>(request: Observable<T>, onSuccess: (value: T) => void): void {
    this.busy.set(true);
    request
      .pipe(finalize(() => this.busy.set(false)))
      .subscribe({ next: onSuccess, error: () => undefined });
  }
}
