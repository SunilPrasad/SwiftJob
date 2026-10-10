export type JobStatus = 'New' | 'Applied' | 'NotRelevant';
export type WorkMode = 'Unknown' | 'Remote' | 'Hybrid' | 'Onsite';
export type SortDirection = 'Asc' | 'Desc';
export type JobSortField = 'ReceivedAt' | 'Title' | 'Company';
export type EmailSyncStatus = 'Pending' | 'Processed' | 'Failed' | 'Ignored';

export const JOB_STATUS_LABELS: Record<JobStatus, string> = {
  New: 'New',
  Applied: 'Applied',
  NotRelevant: 'Not relevant',
};

export const WORK_MODE_LABELS: Record<WorkMode, string> = {
  Unknown: 'Not specified',
  Remote: 'Remote',
  Hybrid: 'Hybrid',
  Onsite: 'On-site',
};

export interface JobListItem {
  id: number;
  receivedAt: string;
  title: string;
  company: string | null;
  salaryText: string | null;
  location: string | null;
  workMode: WorkMode;
  source: string;
  status: JobStatus;
  hasApplicationUrl: boolean;
}

export interface JobEmailSummary {
  id: number;
  gmailMessageId: string;
  subject: string | null;
  sender: string | null;
  receivedAt: string;
  gmailUrl: string;
}

export interface JobDetail {
  id: number;
  title: string;
  company: string | null;
  salaryText: string | null;
  salaryMin: number | null;
  salaryMax: number | null;
  salaryCurrency: string | null;
  location: string | null;
  workMode: WorkMode;
  description: string | null;
  requirements: string | null;
  enrichedDescription: string | null;
  enrichedAt: string | null;
  applicationUrl: string | null;
  source: string;
  sourceJobId: string | null;
  receivedAt: string;
  createdAt: string;
  updatedAt: string;
  status: JobStatus;
  statusChangedAt: string | null;
  emails: JobEmailSummary[];
}

export interface JobEmailDetail {
  id: number;
  gmailMessageId: string;
  subject: string | null;
  sender: string | null;
  receivedAt: string;
  syncStatus: EmailSyncStatus;
  bodyText: string | null;
  gmailUrl: string;
  jobIds: number[];
}

export interface JobStatusCounts {
  new: number;
  applied: number;
  notRelevant: number;
  total: number;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  /** 1-based. */
  page: number;
  pageSize: number;
}

/** Filters shared by the job list and the status counts. */
export interface JobFilter {
  search: string;
  source: string | null;
  /** Inclusive lower bound, ISO 8601 UTC. */
  receivedFrom: string | null;
  /** Exclusive upper bound, ISO 8601 UTC. */
  receivedBefore: string | null;
}

export interface JobListQuery extends JobFilter {
  /** Null means all statuses. */
  status: JobStatus | null;
  sortBy: JobSortField;
  sortDirection: SortDirection;
  /** 1-based. */
  page: number;
  pageSize: number;
}
