import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatToolbarModule } from '@angular/material/toolbar';

@Component({
  selector: 'app-shell',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    RouterOutlet,
    RouterLink,
    RouterLinkActive,
    MatButtonModule,
    MatIconModule,
    MatToolbarModule,
  ],
  template: `
    <mat-toolbar class="toolbar">
      <a routerLink="/jobs" class="brand"><mat-icon>bolt</mat-icon><span>SwiftJob</span></a>
      <nav class="nav">
        <a mat-button routerLink="/jobs" routerLinkActive="active">Jobs</a>
        <a mat-button routerLink="/settings" routerLinkActive="active">Settings</a>
      </nav>
    </mat-toolbar>
    <main class="main"><router-outlet /></main>
  `,
  styles: `
    :host {
      display: flex;
      flex-direction: column;
      height: 100vh;
    }
    .toolbar {
      flex: 0 0 auto;
      gap: 16px;
      background: var(--mat-sys-surface-container);
      border-bottom: 1px solid var(--mat-sys-outline-variant);
    }
    .brand {
      display: flex;
      align-items: center;
      gap: 6px;
      color: var(--mat-sys-primary);
      text-decoration: none;
      font: var(--mat-sys-title-large);
    }
    .nav .active {
      background: var(--mat-sys-secondary-container);
    }
    .main {
      flex: 1;
      min-height: 0;
      overflow: hidden;
    }
  `,
})
export class Shell {}
