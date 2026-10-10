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
