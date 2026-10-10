export type BrowserOwner = 'None' | 'Agent' | 'User';

export interface BrowserStatus {
  isOpen: boolean;
  currentUrl: string | null;
  /** Who may act in the browser: the AI agent or you. */
  controller: BrowserOwner;
  message: string;
}
