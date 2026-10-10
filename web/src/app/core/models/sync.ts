export type SyncState = 'Idle' | 'Running' | 'Completed' | 'Failed';

export interface SyncStatus {
  state: SyncState;
  message: string;
  processed: number;
  total: number;
  newJobs: number;
  startedAt: string | null;
  completedAt: string | null;
}

export type SyncLogLevel = 'Info' | 'Warning' | 'Error';

/** One line of the background activity log. */
export interface SyncLogEntry {
  id: number;
  timestamp: string;
  level: SyncLogLevel;
  message: string;
}
