import { JobStatus } from '../../../core/models/job';

/** Request from a child component to change a job's status. */
export interface JobStatusChange {
  jobId: number;
  status: JobStatus;
  /** Status at the time of the request; used to offer Undo. */
  previousStatus: JobStatus;
  /** Shown in confirmation messages. */
  title: string;
}
