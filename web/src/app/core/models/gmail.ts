export interface GmailSettings {
  hasClientCredentials: boolean;
  clientId: string | null;
  redirectUri: string;
  scope: string;
  isConnected: boolean;
  connectedEmail: string | null;
  connectedAt: string | null;
  searchQuery: string;
  defaultSearchQuery: string;
}

export interface GmailMessagePreview {
  id: string;
  subject: string | null;
  from: string | null;
  receivedAt: string | null;
}

export interface GmailTestResult {
  connectedEmail: string;
  searchQuery: string;
  estimatedMatches: number;
  latestMessages: GmailMessagePreview[];
}
