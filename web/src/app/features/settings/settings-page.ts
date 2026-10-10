import { ChangeDetectionStrategy, Component } from '@angular/core';
import { GmailSettingsCard } from './gmail/gmail-settings-card';

@Component({
  selector: 'app-settings-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [GmailSettingsCard],
  template: `
    <div class="page">
      <h1>Settings</h1>
      <app-gmail-settings-card />
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
    }
    h1 {
      margin: 0 0 16px;
      font: var(--mat-sys-headline-small);
    }
  `,
})
export class SettingsPage {}
