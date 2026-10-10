using Microsoft.EntityFrameworkCore;
using SwiftJob.Domain.Applications;
using SwiftJob.Domain.Candidates;
using SwiftJob.Domain.Emails;
using SwiftJob.Domain.Jobs;

namespace SwiftJob.Application.Abstractions;

/// <summary>
/// Unit of work over the application's data. Implemented by EF Core in Infrastructure;
/// the concrete database provider (SQLite now, PostgreSQL later) is invisible to this layer.
/// </summary>
public interface IAppDbContext
{
    DbSet<Job> Jobs { get; }
    DbSet<JobEmail> JobEmails { get; }
    DbSet<JobEmailLink> JobEmailLinks { get; }
    DbSet<CandidateProfile> CandidateProfiles { get; }
    DbSet<ApplicationSession> ApplicationSessions { get; }
    DbSet<ApplicationActivity> ApplicationActivities { get; }
    DbSet<ApplicationAnswer> ApplicationAnswers { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
