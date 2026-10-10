import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { AiSettings } from '../../../core/models/ai';
import { AiSettingsCard } from './ai-settings-card';

const providers: AiSettings['providers'] = [
  {
    provider: 'Anthropic',
    displayName: 'Anthropic (Claude)',
    requiresApiKey: true,
    requiresEndpoint: false,
    defaultEndpoint: null,
    suggestedModels: ['claude-opus-5-5'],
    apiKeyHelpUrl: 'https://console.anthropic.com/settings/keys',
    notes: '',
  },
  {
    provider: 'OpenAICompatible',
    displayName: 'OpenAI-compatible endpoint',
    requiresApiKey: false,
    requiresEndpoint: true,
    defaultEndpoint: 'http://localhost:11434/v1',
    suggestedModels: [],
    apiKeyHelpUrl: 'https://example.com',
    notes: '',
  },
];

const empty: AiSettings = {
  provider: 'Anthropic',
  model: null,
  endpoint: null,
  hasApiKey: false,
  isConfigured: false,
  providers,
};

describe('AiSettingsCard', () => {
  let fixture: ComponentFixture<AiSettingsCard>;
  let http: HttpTestingController;
  let el: HTMLElement;

  async function render(settings: AiSettings) {
    fixture = TestBed.createComponent(AiSettingsCard);
    el = fixture.nativeElement;
    TestBed.tick();
    http.expectOne('/api/settings/ai').flush(settings);
    await fixture.whenStable();
  }

  const form = () =>
    (
      fixture.componentInstance as unknown as {
        form: { patchValue(v: object): void; controls: Record<string, { value: unknown }> };
      }
    ).form;
  const button = (text: string) =>
    Array.from(el.querySelectorAll('button')).find((b) => b.textContent?.includes(text));

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    http = TestBed.inject(HttpTestingController);
  });

  it('defaults the model to the provider suggestion and requires a key', async () => {
    await render(empty);

    expect(el.textContent).toContain('Not configured');
    expect(form().controls['model'].value).toBe('claude-opus-5-5');

    button('Save')!.click();
    await fixture.whenStable();
    http.expectNone('/api/settings/ai');
    expect(el.textContent).toContain('An API key is required');
  });

  it('saves provider, model and key', async () => {
    await render(empty);
    form().patchValue({ apiKey: 'sk-ant-123' });

    button('Save')!.click();

    const req = http.expectOne((r) => r.method === 'PUT' && r.url === '/api/settings/ai');
    expect(req.request.body).toEqual({
      provider: 'Anthropic',
      model: 'claude-opus-5-5',
      endpoint: null,
      apiKey: 'sk-ant-123',
      clearApiKey: false,
    });
    req.flush({ ...empty, model: 'claude-opus-5-5', hasApiKey: true, isConfigured: true });
    await fixture.whenStable();
    expect(el.textContent).toContain('Using claude-opus-5-5');
  });

  it('shows an endpoint field and the default URL for OpenAI-compatible providers', async () => {
    await render(empty);
    form().patchValue({ provider: 'OpenAICompatible' });
    await fixture.whenStable();

    expect(form().controls['endpoint'].value).toBe('http://localhost:11434/v1');
    expect(el.textContent).toContain('Endpoint URL');
    expect(el.textContent).toContain('API key (optional)');
  });

  it('reports model test results', async () => {
    await render({ ...empty, model: 'claude-opus-5-5', hasApiKey: true, isConfigured: true });

    button('Test model')!.click();
    http.expectOne('/api/settings/ai/test').flush({
      success: false,
      provider: 'Anthropic',
      model: 'claude-opus-5-5',
      message: 'invalid x-api-key',
    });
    await fixture.whenStable();

    expect(el.textContent).toContain('invalid x-api-key');
  });
});
