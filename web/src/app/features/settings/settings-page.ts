import { ChangeDetectionStrategy, Component, inject, viewChild } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { SyncStatusService } from '../../core/realtime/sync-status';
import { AiSettingsCard } from './ai/ai-settings-card';
import { GmailSettingsCard } from './gmail/gmail-settings-card';
import { SampleDataCard } from './sample-data/sample-data-card';

@Component({
  selector: 'app-settings-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [AiSettingsCard, GmailSettingsCard, SampleDataCard],
  template: `
    <div class="page">
      <h1>Settings</h1>
      <app-gmail-settings-card />
      <app-ai-settings-card />
      <app-sample-data-card />
    </div>
  `,
  styles: `
    :host {
      display: block;
      height: 100%;
      overflow-y: auto;
    }
    .page {
      max-width: 860px;
      margin: 0 auto;
      padding: 24px 16px 48px;
      display: flex;
      flex-direction: column;
      gap: 24px;
    }
    h1 {
      margin: 0;
      font: var(--mat-sys-headline-small);
    }
  `,
})
export class SettingsPage {
  private readonly gmailCard = viewChild(GmailSettingsCard);

  constructor() {
    // Refresh "last sync" information when a sync finishes while this page is open.
    const sync = inject(SyncStatusService);
    sync.connect();
    sync.completed$.pipe(takeUntilDestroyed()).subscribe(() => this.gmailCard()?.reload());
  }
}
