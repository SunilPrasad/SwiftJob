import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { GmailSettings } from '../../../core/models/gmail';
import { GmailSettingsCard } from './gmail-settings-card';

const base: GmailSettings = {
  hasClientCredentials: false,
  clientId: null,
  redirectUri: 'http://localhost:5080/api/settings/gmail/callback',
  scope: 'https://www.googleapis.com/auth/gmail.readonly',
  isConnected: false,
  connectedEmail: null,
  connectedAt: null,
  searchQuery: 'label:jobs',
  defaultSearchQuery: 'label:jobs',
  autoSyncEnabled: true,
  syncIntervalMinutes: 30,
  lastSyncCompletedAt: null,
  lastSyncSummary: null,
  lastSyncError: null,
};

describe('GmailSettingsCard', () => {
  let fixture: ComponentFixture<GmailSettingsCard>;
  let http: HttpTestingController;
  let el: HTMLElement;

  async function render(settings: GmailSettings) {
    fixture = TestBed.createComponent(GmailSettingsCard);
    el = fixture.nativeElement;
    TestBed.tick(); // starts the settings request
    http.expectOne('/api/settings/gmail').flush(settings);
    await fixture.whenStable();
  }

  const buttons = () => Array.from(el.querySelectorAll('button'));
  const button = (text: string) => buttons().find((b) => b.textContent?.includes(text));

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    });
    http = TestBed.inject(HttpTestingController);
  });

  it('asks for client credentials and keeps Connect disabled until they are saved', async () => {
    await render(base);

    expect(el.textContent).toContain('Not set up');
    expect(el.querySelector('input[formControlName="clientSecret"]')?.getAttribute('type')).toBe(
      'password',
    );
    expect(button('Connect Gmail')?.disabled).toBe(true);
    expect(el.textContent).toContain('gmail.readonly');
  });

  it('saves credentials via the API', async () => {
    await render(base);
    const component = fixture.componentInstance as unknown as {
      credentialsForm: { setValue(v: object): void };
    };
    component.credentialsForm.setValue({
      clientId: 'abc.apps.googleusercontent.com',
      clientSecret: 'GOCSPX-123456',
    });
    button('Save client')!.click();

    const req = http.expectOne('/api/settings/gmail/credentials');
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual({
      clientId: 'abc.apps.googleusercontent.com',
      clientSecret: 'GOCSPX-123456',
    });
    req.flush({ ...base, hasClientCredentials: true, clientId: 'abc.apps.googleusercontent.com' });
    await fixture.whenStable();

    expect(el.textContent).toContain('Ready to connect');
    expect(button('Connect Gmail')?.disabled).toBe(false);
  });

  it('shows the connected account with test and disconnect actions', async () => {
    await render({
      ...base,
      hasClientCredentials: true,
      clientId: 'abc.apps.googleusercontent.com',
      isConnected: true,
      connectedEmail: 'me@gmail.com',
      connectedAt: '2026-10-01T10:00:00Z',
    });

    expect(el.textContent).toContain('Connected as me@gmail.com');
    expect(button('Test connection')).toBeTruthy();
    expect(button('Disconnect')).toBeTruthy();
    expect(el.textContent).toContain('Disconnect Gmail to change the client');
  });

  it('displays test results', async () => {
    await render({
      ...base,
      hasClientCredentials: true,
      clientId: 'x'.repeat(12),
      isConnected: true,
      connectedEmail: 'me@gmail.com',
    });
    button('Test connection')!.click();

    http.expectOne('/api/settings/gmail/test').flush({
      connectedEmail: 'me@gmail.com',
      searchQuery: 'label:jobs',
      estimatedMatches: 12,
      latestMessages: [
        {
          id: '1',
          subject: '3 new jobs for you',
          from: 'LinkedIn',
          receivedAt: '2026-10-01T10:00:00Z',
        },
      ],
    });
    await fixture.whenStable();

    expect(el.textContent).toContain('About 12 emails match');
    expect(el.textContent).toContain('3 new jobs for you');
  });
});
