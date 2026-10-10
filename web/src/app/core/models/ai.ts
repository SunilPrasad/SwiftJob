export type AiProvider = 'Anthropic' | 'OpenAI' | 'OpenAICompatible';

/** Provider description supplied by the backend, so the UI holds no provider-specific knowledge. */
export interface AiProviderInfo {
  provider: AiProvider;
  displayName: string;
  requiresApiKey: boolean;
  requiresEndpoint: boolean;
  defaultEndpoint: string | null;
  suggestedModels: string[];
  apiKeyHelpUrl: string;
  notes: string;
}

export interface AiSettings {
  provider: AiProvider;
  model: string | null;
  endpoint: string | null;
  hasApiKey: boolean;
  isConfigured: boolean;
  providers: AiProviderInfo[];
}

export interface SaveAiSettings {
  provider: AiProvider;
  model: string;
  endpoint: string | null;
  /** Null keeps the stored key. */
  apiKey: string | null;
  clearApiKey: boolean;
}

export interface AiTestResult {
  success: boolean;
  provider: string;
  model: string;
  message: string;
}
